using Microsoft.EntityFrameworkCore;
using TeamActivity.Domain;

namespace TeamActivity.Infrastructure;

public sealed class TeamActivityDbContext(DbContextOptions<TeamActivityDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceEnrollment> DeviceEnrollments => Set<DeviceEnrollment>();
    public DbSet<DeviceCertificate> DeviceCertificates => Set<DeviceCertificate>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<PolicyAcceptance> PolicyAcceptances => Set<PolicyAcceptance>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<WorkSession> WorkSessions => Set<WorkSession>();
    public DbSet<ActivityBucket> ActivityBuckets => Set<ActivityBucket>();
    public DbSet<TimeSegment> TimeSegments => Set<TimeSegment>();
    public DbSet<TimeCorrection> TimeCorrections => Set<TimeCorrection>();
    public DbSet<ScreenshotSet> ScreenshotSets => Set<ScreenshotSet>();
    public DbSet<ScreenshotAsset> ScreenshotAssets => Set<ScreenshotAsset>();
    public DbSet<Heartbeat> Heartbeats => Set<Heartbeat>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<QualityReview> QualityReviews => Set<QualityReview>();
    public DbSet<ReviewComment> ReviewComments => Set<ReviewComment>();
    public DbSet<RemoteSupportRequest> RemoteSupportRequests => Set<RemoteSupportRequest>();
    public DbSet<RemoteSupportSession> RemoteSupportSessions => Set<RemoteSupportSession>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>().HasIndex(x => x.Email);
        modelBuilder.Entity<Device>().HasIndex(x => x.InstallationIdHash).IsUnique();
        modelBuilder.Entity<Device>().HasIndex(x => new { x.EmployeeId, x.State });
        modelBuilder.Entity<DeviceEnrollment>().HasIndex(x => x.CodeHash).IsUnique();
        modelBuilder.Entity<DeviceCertificate>().HasIndex(x => x.Thumbprint).IsUnique();
        modelBuilder.Entity<Policy>().HasIndex(x => x.Version).IsUnique();
        modelBuilder.Entity<PolicyAcceptance>().HasIndex(x => new { x.DeviceId, x.PolicyVersion }).IsUnique();
        modelBuilder.Entity<ActivityBucket>().HasIndex(x => new { x.DeviceId, x.StartedAtUtc }).IsUnique();
        modelBuilder.Entity<TimeSegment>().HasIndex(x => new { x.EmployeeId, x.StartedAtUtc });
        modelBuilder.Entity<ScreenshotSet>().HasIndex(x => x.IdempotencyKey).IsUnique();
        modelBuilder.Entity<ScreenshotSet>().HasIndex(x => x.RetainUntilUtc);
        modelBuilder.Entity<ScreenshotAsset>().HasIndex(x => x.ContentHash);
        modelBuilder.Entity<Heartbeat>().HasIndex(x => new { x.DeviceId, x.ReceivedAtUtc });
        modelBuilder.Entity<Alert>().HasIndex(x => new { x.DeviceId, x.ResolvedAtUtc });
        modelBuilder.Entity<AuditEvent>().HasIndex(x => x.CreatedAtUtc);
        modelBuilder.Entity<RetentionPolicy>().HasIndex(x => x.DataType).IsUnique();

        modelBuilder.Entity<Employee>()
            .HasMany(x => x.Devices)
            .WithOne(x => x.Employee)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ScreenshotSet>()
            .HasMany(x => x.Assets)
            .WithOne(x => x.ScreenshotSet)
            .HasForeignKey(x => x.ScreenshotSetId)
            .OnDelete(DeleteBehavior.Cascade);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(x => typeof(Entity).IsAssignableFrom(x.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Property(nameof(Entity.RowVersion)).IsConcurrencyToken();
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(
                BuildNotDeletedFilter(entityType.ClrType));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private static System.Linq.Expressions.LambdaExpression BuildNotDeletedFilter(Type type)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(type, "e");
        var property = System.Linq.Expressions.Expression.Property(parameter, nameof(Entity.IsDeleted));
        var body = System.Linq.Expressions.Expression.Equal(
            property,
            System.Linq.Expressions.Expression.Constant(false));
        return System.Linq.Expressions.Expression.Lambda(body, parameter);
    }

    private void UpdateAuditFields()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
