using Microsoft.EntityFrameworkCore;
using QueryMind.Domain.Interfaces;
using QueryMind.Infrastructure.Persistence;

namespace QueryMind.Infrastructure.Services;

/// <summary>
/// Simple DB-backed schema context — returns the stored ParsedJson directly.
/// Replaces Azure AI Search for deployments that don't need vector RAG.
/// </summary>
public class SimpleSchemaSearchService(QueryMindDbContext db) : ISchemaSearchService
{
    private const int MaxContextChars = 12_000;

    // No-op: schema is already persisted as ParsedJson in the DB
    public Task IndexSchemaAsync(Guid schemaId, Guid tenantId, SchemaContext context, CancellationToken ct = default)
        => Task.CompletedTask;

    public async Task<string> SearchSchemaContextAsync(Guid schemaId, string query, int topK = 5, CancellationToken ct = default)
    {
        var schema = await db.Schemas
            .Where(s => s.Id == schemaId)
            .Select(s => new { s.ParsedJson, s.Name, s.Type })
            .FirstOrDefaultAsync(ct);

        if (schema?.ParsedJson == null) return string.Empty;

        var context = $"Schema Name: {schema.Name}\nType: {schema.Type}\n\n{schema.ParsedJson}";

        // Truncate to avoid token overflow on very large schemas
        return context.Length > MaxContextChars
            ? context[..MaxContextChars] + "\n...(schema truncated — upload a smaller schema for full context)"
            : context;
    }

    // No-op: schema is soft-deleted in the DB; no external index to clean up
    public Task DeleteSchemaIndexAsync(Guid schemaId, CancellationToken ct = default)
        => Task.CompletedTask;
}
