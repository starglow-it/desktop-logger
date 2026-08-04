using System.Net;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TeamActivity.Contracts;
using TeamActivity.Domain;
using TeamActivity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService(options => options.ServiceName = "TeamActivity Manager Server");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
builder.Services.AddSignalR();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var dataRoot = Path.GetFullPath(builder.Configuration["DataRoot"] ??
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TeamActivity"));
Directory.CreateDirectory(dataRoot);
var databasePath = Path.Combine(dataRoot, "teamactivity.db");
builder.Services.AddDbContext<TeamActivityDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath};Cache=Shared;Pooling=True"));
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<AuditWriter>();
builder.Services.AddSingleton<IClock, SystemClock>();

var app = builder.Build();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Cache-Control"] = "no-store";
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'");
    await next();
});

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TeamActivity.Manager.Server" }));

var api = app.MapGroup("/api").RequireRateLimiting("api");

api.MapPost("/enrollment/codes", async (
    CreateEnrollmentCodeRequest request,
    HttpContext context,
    TeamActivityDbContext db,
    AuditWriter audit,
    IClock clock,
    CancellationToken cancellationToken) =>
{
    if (!RequestSecurity.IsLocalManager(context)) return Results.Forbid();
    if (request.LifetimeMinutes is < 1 or > 30)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["lifetimeMinutes"] = ["Must be between 1 and 30."] });

    var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(10));
    var enrollment = new DeviceEnrollment
    {
        CodeHash = SecretHasher.Hash(code),
        ExpiresAtUtc = clock.UtcNow.AddMinutes(request.LifetimeMinutes)
    };
    db.DeviceEnrollments.Add(enrollment);
    await db.SaveChangesAsync(cancellationToken);
    await audit.WriteAsync("local-manager", "EnrollmentCodeCreated", "DeviceEnrollment", enrollment.Id.ToString(),
        new { enrollment.ExpiresAtUtc }, cancellationToken);
    return Results.Ok(new EnrollmentCodeResponse(code, enrollment.ExpiresAtUtc));
});

api.MapPost("/employees", async (
    CreateEmployeeRequest request,
    HttpContext context,
    TeamActivityDbContext db,
    AuditWriter audit,
    CancellationToken cancellationToken) =>
{
    if (!RequestSecurity.IsLocalManager(context)) return Results.Forbid();
    var displayName = request.DisplayName.Trim();
    if (displayName.Length is < 1 or > 120)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["displayName"] = ["Must be between 1 and 120 characters."] });

    var employee = new Employee { DisplayName = displayName, Email = request.Email?.Trim() };
    db.Employees.Add(employee);
    await db.SaveChangesAsync(cancellationToken);
    await audit.WriteAsync("local-manager", "EmployeeCreated", "Employee", employee.Id.ToString(),
        new { employee.DisplayName }, cancellationToken);
    return Results.Created($"/api/employees/{employee.Id}", new EmployeeResponse(employee.Id, employee.DisplayName, employee.Email));
});

api.MapPost("/enrollment", async (
    EnrollDeviceRequest request,
    TeamActivityDbContext db,
    AuditWriter audit,
    IClock clock,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.InstallationId))
        return Results.BadRequest(new ApiError("invalid_enrollment", "Code and installation ID are required."));

    var codeHash = SecretHasher.Hash(request.Code.Trim());
    var enrollment = await db.DeviceEnrollments.SingleOrDefaultAsync(x => x.CodeHash == codeHash, cancellationToken);
    if (enrollment is null || enrollment.ConsumedAtUtc is not null || enrollment.ExpiresAtUtc <= clock.UtcNow)
        return Results.BadRequest(new ApiError("invalid_enrollment", "The enrollment code is invalid, expired, or already used."));

    var installationIdHash = SecretHasher.Hash(request.InstallationId.Trim());
    if (await db.Devices.AnyAsync(x => x.InstallationIdHash == installationIdHash, cancellationToken))
        return Results.Conflict(new ApiError("already_enrolled", "This installation is already enrolled."));

    var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    var device = new Device
    {
        InstallationIdHash = installationIdHash,
        TokenHash = SecretHasher.Hash(token),
        ApplicationVersion = request.ApplicationVersion.Trim(),
        State = DeviceState.Pending
    };
    db.Devices.Add(device);
    db.PolicyAcceptances.Add(new PolicyAcceptance
    {
        DeviceId = device.Id,
        PolicyVersion = request.PolicyVersion.Trim(),
        AcceptedAtUtc = request.AcceptedAtUtc
    });
    enrollment.ConsumedAtUtc = clock.UtcNow;
    enrollment.DeviceId = device.Id;
    await db.SaveChangesAsync(cancellationToken);
    await audit.WriteAsync("device-enrollment", "DeviceEnrolled", "Device", device.Id.ToString(),
        new { request.PolicyVersion, request.AcceptedAtUtc, request.ApplicationVersion }, cancellationToken);
    return Results.Ok(new EnrollDeviceResponse(device.Id, DeviceConnectionState.Pending, token, request.PolicyVersion));
});

api.MapPost("/devices/{deviceId:guid}/approve", async (
    Guid deviceId,
    ApproveDeviceRequest request,
    HttpContext context,
    TeamActivityDbContext db,
    AuditWriter audit,
    IClock clock,
    CancellationToken cancellationToken) =>
{
    if (!RequestSecurity.IsLocalManager(context)) return Results.Forbid();
    var device = await db.Devices.SingleOrDefaultAsync(x => x.Id == deviceId, cancellationToken);
    if (device is null) return Results.NotFound();
    if (!await db.Employees.AnyAsync(x => x.Id == request.EmployeeId, cancellationToken))
        return Results.BadRequest(new ApiError("invalid_employee", "The selected employee does not exist."));

    device.EmployeeId = request.EmployeeId;
    device.ApprovedAtUtc = clock.UtcNow;
    device.ApprovedBy = request.ApprovedBy.Trim();
    device.State = DeviceState.Online;
    await db.SaveChangesAsync(cancellationToken);
    await audit.WriteAsync(request.ApprovedBy, "DeviceApproved", "Device", device.Id.ToString(),
        new { request.EmployeeId }, cancellationToken);
    return Results.NoContent();
});

api.MapPost("/heartbeats", async (
    HeartbeatRequest request,
    HttpContext context,
    TeamActivityDbContext db,
    IHubContext<DashboardHub> hub,
    IClock clock,
    CancellationToken cancellationToken) =>
{
    var device = await RequestSecurity.AuthenticateDeviceAsync(context, db, request.DeviceId, cancellationToken);
    if (device is null) return Results.Unauthorized();
    if (request.PendingUploadCount < 0) return Results.BadRequest(new ApiError("invalid_queue_count", "Queue count cannot be negative."));

    device.LastHeartbeatUtc = clock.UtcNow;
    device.ApplicationVersion = request.ApplicationVersion.Trim();
    device.State = DeviceState.Online;
    db.Heartbeats.Add(new Heartbeat
    {
        DeviceId = device.Id,
        SentAtUtc = request.SentAtUtc,
        ReceivedAtUtc = clock.UtcNow,
        ApplicationVersion = request.ApplicationVersion.Trim(),
        Health = request.Health.Trim(),
        PendingUploadCount = request.PendingUploadCount
    });
    await db.SaveChangesAsync(cancellationToken);
    await hub.Clients.All.SendAsync("DeviceUpdated", device.Id, cancellationToken);

    var policy = await db.Policies.SingleAsync(x => x.IsActive, cancellationToken);
    return Results.Ok(new HeartbeatResponse(clock.UtcNow, MapState(device.State), policy.Version));
});

api.MapPost("/activity-buckets", async (
    ActivityBucketUpload request,
    HttpContext context,
    TeamActivityDbContext db,
    CancellationToken cancellationToken) =>
{
    var device = await RequestSecurity.AuthenticateDeviceAsync(context, db, request.DeviceId, cancellationToken);
    if (device is null) return Results.Unauthorized();
    if (request.EndedAtUtc <= request.StartedAtUtc || request.EndedAtUtc - request.StartedAtUtc > TimeSpan.FromMinutes(2) ||
        request.KeyboardEventCount < 0 || request.MouseEventCount < 0 || request.MouseDistancePixels < 0)
        return Results.BadRequest(new ApiError("invalid_activity_bucket", "The activity bucket failed validation."));

    var bucket = new ActivityBucket
    {
        DeviceId = request.DeviceId,
        StartedAtUtc = request.StartedAtUtc,
        EndedAtUtc = request.EndedAtUtc,
        State = Enum.Parse<ActivityKind>(request.State.ToString()),
        LastKeyboardInputAtUtc = request.LastKeyboardInputAtUtc,
        LastMouseInputAtUtc = request.LastMouseInputAtUtc,
        KeyboardEventCount = request.KeyboardEventCount,
        MouseEventCount = request.MouseEventCount,
        MouseDistancePixels = request.MouseDistancePixels
    };
    db.ActivityBuckets.Add(bucket);
    try
    {
        await db.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException)
    {
        return Results.Conflict(new ApiError("duplicate_bucket", "This activity bucket was already accepted."));
    }
    return Results.Accepted();
});

api.MapGet("/devices", async (
    HttpContext context,
    TeamActivityDbContext db,
    IClock clock,
    CancellationToken cancellationToken) =>
{
    if (!RequestSecurity.IsLocalManager(context)) return Results.Forbid();
    var devices = await db.Devices.Include(x => x.Employee).OrderBy(x => x.Employee!.DisplayName).ToListAsync(cancellationToken);
    var alerts = await db.Alerts.Where(x => x.ResolvedAtUtc == null && x.DeviceId != null)
        .GroupBy(x => x.DeviceId!.Value).Select(x => new { DeviceId = x.Key, Count = x.Count() })
        .ToDictionaryAsync(x => x.DeviceId, x => x.Count, cancellationToken);

    var summaries = devices.Select(device =>
    {
        var state = device.State == DeviceState.Revoked
            ? DeviceState.Revoked
            : HeartbeatStatusEvaluator.Evaluate(clock.UtcNow, device.LastHeartbeatUtc, TimeSpan.FromSeconds(30));
        return new DeviceSummary(
            device.Id,
            device.Employee?.DisplayName ?? "Pending assignment",
            MapState(state),
            device.LastHeartbeatUtc,
            device.ApplicationVersion,
            alerts.GetValueOrDefault(device.Id),
            TeamActivity.Contracts.ActivityState.Unknown);
    });
    return Results.Ok(summaries);
});

api.MapGet("/policy", async (TeamActivityDbContext db, CancellationToken cancellationToken) =>
{
    var policy = await db.Policies.SingleAsync(x => x.IsActive, cancellationToken);
    return Results.Ok(new PolicySummary(
        policy.Version,
        policy.ScreenshotMinimumMinutes,
        policy.ScreenshotMaximumMinutes,
        policy.IdleThresholdMinutes,
        policy.ScreenshotRetentionDays,
        policy.RemoteSupportEnabled));
});

api.MapPost("/screenshots", () => Results.Json(
    new ApiError("not_enabled", "Screenshot ingestion is disabled in v0.1 pending capture-consent and certificate review."),
    statusCode: StatusCodes.Status501NotImplemented));

app.MapHub<DashboardHub>("/hubs/dashboard");
app.Run();

static DeviceConnectionState MapState(DeviceState state) => state switch
{
    DeviceState.Pending => DeviceConnectionState.Pending,
    DeviceState.Online => DeviceConnectionState.Online,
    DeviceState.Degraded => DeviceConnectionState.Degraded,
    DeviceState.Offline => DeviceConnectionState.Offline,
    DeviceState.Revoked => DeviceConnectionState.Revoked,
    _ => DeviceConnectionState.Offline
};

public sealed class DashboardHub : Hub
{
}

internal static class RequestSecurity
{
    public static bool IsLocalManager(HttpContext context) =>
        context.Connection.RemoteIpAddress is not null && IPAddress.IsLoopback(context.Connection.RemoteIpAddress);

    public static async Task<Device?> AuthenticateDeviceAsync(
        HttpContext context,
        TeamActivityDbContext db,
        Guid expectedDeviceId,
        CancellationToken cancellationToken)
    {
        var header = context.Request.Headers["Authorization"].ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var token = header[7..].Trim();
        if (token.Length < 32) return null;
        var tokenHash = SecretHasher.Hash(token);
        return await db.Devices.SingleOrDefaultAsync(
            x => x.Id == expectedDeviceId && x.TokenHash == tokenHash && x.State != DeviceState.Revoked,
            cancellationToken);
    }
}

public partial class Program
{
}
