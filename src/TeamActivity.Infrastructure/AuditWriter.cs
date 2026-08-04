using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TeamActivity.Domain;

namespace TeamActivity.Infrastructure;

public sealed class AuditWriter(TeamActivityDbContext dbContext)
{
    public async Task<AuditEvent> WriteAsync(
        string actor,
        string action,
        string subjectType,
        string subjectId,
        object details,
        CancellationToken cancellationToken = default)
    {
        var previousHash = await dbContext.AuditEvents
            .IgnoreQueryFilters()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => x.Hash)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var auditEvent = new AuditEvent
        {
            Actor = actor,
            Action = action,
            SubjectType = subjectType,
            SubjectId = subjectId,
            DetailsJson = JsonSerializer.Serialize(details),
            PreviousHash = previousHash
        };
        auditEvent.Hash = AuditChain.CalculateHash(auditEvent);
        dbContext.AuditEvents.Add(auditEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        return auditEvent;
    }
}
