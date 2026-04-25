using QueryMind.Domain.Entities;
using QueryMind.Domain.Interfaces;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.Infrastructure.Services;

public class AuditService(QueryMindDbContext db) : IAuditService
{
    public async Task LogAsync(Guid tenantId, string? userId, string action, string entityType,
        string? entityId = null, string? details = null, string? ipAddress = null, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress
        });
        await db.SaveChangesAsync(ct);
    }
}
