namespace QueryMind.Domain.Interfaces;

public interface IAuditService
{
    Task LogAsync(Guid tenantId, string? userId, string action, string entityType, string? entityId = null, string? details = null, string? ipAddress = null, CancellationToken ct = default);
}
