using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TeamActivity.Domain;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public static class ScreenshotIntervalScheduler
{
    public static TimeSpan Next(int minimumMinutes, int maximumMinutes)
    {
        if (minimumMinutes <= 0 || maximumMinutes < minimumMinutes)
            throw new ArgumentOutOfRangeException(nameof(minimumMinutes));

        var seconds = RandomNumberGenerator.GetInt32(
            checked(minimumMinutes * 60),
            checked(maximumMinutes * 60 + 1));
        return TimeSpan.FromSeconds(seconds);
    }

    public static TimeSpan Next(int minimumMinutes, int maximumMinutes, Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (minimumMinutes <= 0 || maximumMinutes < minimumMinutes)
            throw new ArgumentOutOfRangeException(nameof(minimumMinutes));

        return TimeSpan.FromSeconds(random.Next(
            checked(minimumMinutes * 60),
            checked(maximumMinutes * 60 + 1)));
    }
}

public static class ActivityStateCalculator
{
    public static ActivityKind Classify(
        DateTimeOffset bucketStartUtc,
        DateTimeOffset bucketEndUtc,
        DateTimeOffset evaluatedAtUtc,
        DateTimeOffset? lastInputUtc,
        TimeSpan idleThreshold,
        bool withinSchedule,
        bool isLocked,
        bool isSignedIn,
        bool isApprovedBreak,
        bool telemetryComplete = true)
    {
        if (bucketEndUtc <= bucketStartUtc) throw new ArgumentException("Bucket end must be after start.");
        if (idleThreshold <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(idleThreshold));
        if (!telemetryComplete) return ActivityKind.Unknown;
        if (!isSignedIn) return ActivityKind.SignedOut;
        if (isLocked) return ActivityKind.Locked;
        if (isApprovedBreak) return ActivityKind.ApprovedBreak;
        if (!withinSchedule) return ActivityKind.Unknown;
        if (lastInputUtc is null) return ActivityKind.Unknown;

        if (lastInputUtc.Value >= bucketStartUtc && lastInputUtc.Value < bucketEndUtc)
            return ActivityKind.Active;

        var inactivityAtEvaluation = evaluatedAtUtc - lastInputUtc.Value;
        if (inactivityAtEvaluation >= idleThreshold && bucketStartUtc > lastInputUtc.Value)
            return ActivityKind.Idle;

        return ActivityKind.Active;
    }
}

public static class HeartbeatStatusEvaluator
{
    public static DeviceState Evaluate(DateTimeOffset nowUtc, DateTimeOffset? lastHeartbeatUtc, TimeSpan interval)
    {
        if (lastHeartbeatUtc is null) return DeviceState.Pending;
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

        var elapsed = nowUtc - lastHeartbeatUtc.Value;
        if (elapsed >= interval * 3) return DeviceState.Offline;
        if (elapsed >= interval * 2) return DeviceState.Degraded;
        return DeviceState.Online;
    }
}

public static class WorkScheduleEvaluator
{
    public static bool IsWithinSchedule(
        DateTimeOffset instantUtc,
        DayOfWeek scheduledDay,
        TimeOnly startsAtLocal,
        TimeOnly endsAtLocal,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        var local = TimeZoneInfo.ConvertTime(instantUtc, timeZone);
        var localTime = TimeOnly.FromDateTime(local.DateTime);
        if (startsAtLocal <= endsAtLocal)
            return local.DayOfWeek == scheduledDay && localTime >= startsAtLocal && localTime < endsAtLocal;

        if (localTime >= startsAtLocal) return local.DayOfWeek == scheduledDay;
        if (localTime < endsAtLocal) return local.AddDays(-1).DayOfWeek == scheduledDay;
        return false;
    }
}

public sealed record MonitorCaptureMetadata(
    int MonitorNumber,
    int Width,
    int Height,
    double Scale,
    int OrientationDegrees,
    DateTimeOffset CapturedAtUtc);

public static class ScreenshotSetRules
{
    public static void Validate(IReadOnlyCollection<MonitorCaptureMetadata> monitors, TimeSpan maximumSkew)
    {
        ArgumentNullException.ThrowIfNull(monitors);
        if (monitors.Count == 0) throw new ArgumentException("At least one monitor is required.", nameof(monitors));
        if (maximumSkew < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maximumSkew));
        if (monitors.Select(x => x.MonitorNumber).Distinct().Count() != monitors.Count)
            throw new ArgumentException("Monitor numbers must be unique within a screenshot set.", nameof(monitors));
        if (monitors.Any(x => x.Width <= 0 || x.Height <= 0 || x.Scale <= 0 ||
                              (x.OrientationDegrees is not 0 and not 90 and not 180 and not 270)))
            throw new ArgumentException("Monitor metadata is invalid.", nameof(monitors));

        var first = monitors.Min(x => x.CapturedAtUtc);
        var last = monitors.Max(x => x.CapturedAtUtc);
        if (last - first > maximumSkew)
            throw new ArgumentException("Monitor captures are too far apart to form one screenshot set.", nameof(monitors));
    }
}

public static class SecretHasher
{
    public static string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    public static bool Verify(string value, string expectedHash) =>
        CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(Hash(value)),
            Convert.FromHexString(expectedHash));
}

public static class AuditChain
{
    public static string CalculateHash(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        var canonical = JsonSerializer.Serialize(new
        {
            auditEvent.Id,
            auditEvent.CreatedAtUtc,
            auditEvent.Actor,
            auditEvent.Action,
            auditEvent.SubjectType,
            auditEvent.SubjectId,
            auditEvent.DetailsJson,
            auditEvent.PreviousHash
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public static bool IsValid(AuditEvent auditEvent) =>
        string.Equals(auditEvent.Hash, CalculateHash(auditEvent), StringComparison.Ordinal);
}
