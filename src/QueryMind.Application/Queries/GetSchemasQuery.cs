using MediatR;
using Microsoft.EntityFrameworkCore;
using QueryMind.Application.DTOs;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.Application.Queries;

public record GetSchemasQuery(Guid TenantId) : IRequest<IReadOnlyList<SchemaDto>>;

public class GetSchemasQueryHandler(QueryMindDbContext db) : IRequestHandler<GetSchemasQuery, IReadOnlyList<SchemaDto>>
{
    public async Task<IReadOnlyList<SchemaDto>> Handle(GetSchemasQuery request, CancellationToken ct)
    {
        return await db.Schemas
            .Where(s => s.TenantId == request.TenantId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SchemaDto(s.Id, s.Name, s.Type, s.FileSizeBytes, s.IsProcessed, s.Description, s.ProcessingError, s.CreatedAt))
            .ToListAsync(ct);
    }
}
