using Microsoft.EntityFrameworkCore;
using TeamActivity.Domain;

namespace TeamActivity.Infrastructure;

public sealed class DatabaseInitializer(TeamActivityDbContext dbContext)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        SQLitePCL.Batteries_V2.Init();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;", cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;", cancellationToken);

        if (!await dbContext.Policies.AnyAsync(cancellationToken))
        {
            dbContext.Policies.Add(new Policy());
            dbContext.RetentionPolicies.AddRange(
                new RetentionPolicy { DataType = "FullScreenshots", RetentionDays = 30 },
                new RetentionPolicy { DataType = "Thumbnails", RetentionDays = 30 },
                new RetentionPolicy { DataType = "ActivityBuckets", RetentionDays = 90 },
                new RetentionPolicy { DataType = "Timesheets", RetentionDays = 365 },
                new RetentionPolicy { DataType = "Alerts", RetentionDays = 180 },
                new RetentionPolicy { DataType = "QualityReviews", RetentionDays = 730 },
                new RetentionPolicy { DataType = "AuditRecords", RetentionDays = 2555 });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
