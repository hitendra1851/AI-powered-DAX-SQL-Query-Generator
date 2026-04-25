using MediatR;
using QueryMind.Domain.Interfaces;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.Application.Commands;

public record DeleteSchemaCommand(Guid TenantId, Guid SchemaId) : IRequest;

public class DeleteSchemaCommandHandler(
    QueryMindDbContext db,
    IStorageService storage,
    ISchemaSearchService search,
    IAuditService audit
) : IRequestHandler<DeleteSchemaCommand>
{
    public async Task Handle(DeleteSchemaCommand request, CancellationToken ct)
    {
        var schema = await db.Schemas.FindAsync([request.SchemaId], ct)
            ?? throw new InvalidOperationException("Schema not found");

        if (schema.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied");

        schema.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        if (schema.FileUrl != null)
            await storage.DeleteAsync(schema.FileUrl, ct);

        await search.DeleteSchemaIndexAsync(request.SchemaId, ct);
        await audit.LogAsync(request.TenantId, null, "SchemaDeleted", "Schema", request.SchemaId.ToString(), null, null, ct);
    }
}
