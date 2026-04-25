using MediatR;
using Microsoft.EntityFrameworkCore;
using QueryMind.Application.DTOs;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.Application.Queries;

public record GetTenantUsageQuery(Guid TenantId) : IRequest<TenantUsageDto>;

public class GetTenantUsageQueryHandler(QueryMindDbContext db) : IRequestHandler<GetTenantUsageQuery, TenantUsageDto>
{
    public async Task<TenantUsageDto> Handle(GetTenantUsageQuery request, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync([request.TenantId], ct)
            ?? throw new InvalidOperationException("Tenant not found");

        var totalSessions = await db.Sessions.CountAsync(s => s.TenantId == request.TenantId, ct);
        var totalSchemas = await db.Schemas.CountAsync(s => s.TenantId == request.TenantId, ct);

        var tokenStats = await db.Messages
            .Where(m => m.Session.TenantId == request.TenantId)
            .GroupBy(_ => 1)
            .Select(g => new { InputTokens = (long)g.Sum(m => m.InputTokens), OutputTokens = (long)g.Sum(m => m.OutputTokens) })
            .FirstOrDefaultAsync(ct);

        return new TenantUsageDto(
            tenant.Id,
            tenant.Name,
            tenant.Plan,
            tenant.MonthlyQueryCount,
            tenant.GetQueryLimit(),
            totalSessions,
            totalSchemas,
            tokenStats?.InputTokens ?? 0,
            tokenStats?.OutputTokens ?? 0,
            tenant.QueryCountResetAt
        );
    }
}
