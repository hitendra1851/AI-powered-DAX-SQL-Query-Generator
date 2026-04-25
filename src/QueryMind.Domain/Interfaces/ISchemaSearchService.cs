using QueryMind.Domain.Interfaces;

namespace QueryMind.Domain.Interfaces;

public interface ISchemaSearchService
{
    Task IndexSchemaAsync(Guid schemaId, Guid tenantId, SchemaContext context, CancellationToken ct = default);
    Task<string> SearchSchemaContextAsync(Guid schemaId, string query, int topK = 5, CancellationToken ct = default);
    Task DeleteSchemaIndexAsync(Guid schemaId, CancellationToken ct = default);
}
