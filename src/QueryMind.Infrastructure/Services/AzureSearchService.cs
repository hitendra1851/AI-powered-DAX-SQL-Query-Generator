using System.Text;
using System.Text.Json;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using QueryMind.Domain.Interfaces;

namespace QueryMind.Infrastructure.Services;

public class AzureSearchService(IConfiguration configuration) : ISchemaSearchService
{
    private readonly string _endpoint = configuration["Azure:SearchEndpoint"]
        ?? throw new InvalidOperationException("Azure:SearchEndpoint is required");
    private readonly string _apiKey = configuration["Azure:SearchApiKey"]
        ?? throw new InvalidOperationException("Azure:SearchApiKey is required");
    private const string IndexName = "querymind-schemas";

    public async Task IndexSchemaAsync(Guid schemaId, Guid tenantId, SchemaContext context, CancellationToken ct = default)
    {
        var indexClient = new SearchIndexClient(new Uri(_endpoint), new Azure.AzureKeyCredential(_apiKey));
        await EnsureIndexExistsAsync(indexClient, ct);

        var searchClient = new SearchClient(new Uri(_endpoint), IndexName, new Azure.AzureKeyCredential(_apiKey));
        var documents = BuildDocuments(schemaId, tenantId, context);

        await searchClient.UploadDocumentsAsync(documents, cancellationToken: ct);
    }

    public async Task<string> SearchSchemaContextAsync(Guid schemaId, string query, int topK = 5, CancellationToken ct = default)
    {
        var searchClient = new SearchClient(new Uri(_endpoint), IndexName, new Azure.AzureKeyCredential(_apiKey));

        var options = new SearchOptions
        {
            Filter = $"schemaId eq '{schemaId}'",
            Size = topK,
            Select = { "content", "chunkType" }
        };

        var results = await searchClient.SearchAsync<SchemaDocument>(query, options, ct);
        var sb = new StringBuilder();

        await foreach (var result in results.Value.GetResultsAsync())
        {
            sb.AppendLine(result.Document.Content);
            sb.AppendLine("---");
        }

        return sb.ToString();
    }

    public async Task DeleteSchemaIndexAsync(Guid schemaId, CancellationToken ct = default)
    {
        var searchClient = new SearchClient(new Uri(_endpoint), IndexName, new Azure.AzureKeyCredential(_apiKey));
        var results = await searchClient.SearchAsync<SchemaDocument>("*",
            new SearchOptions { Filter = $"schemaId eq '{schemaId}'", Select = { "id" } }, ct);

        var ids = new List<string>();
        await foreach (var r in results.Value.GetResultsAsync())
            ids.Add(r.Document.Id);

        if (ids.Count > 0)
            await searchClient.DeleteDocumentsAsync("id", ids, cancellationToken: ct);
    }

    private static List<SchemaDocument> BuildDocuments(Guid schemaId, Guid tenantId, SchemaContext context)
    {
        var docs = new List<SchemaDocument>();

        foreach (var table in context.Tables)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Table: {table.Name}");
            if (table.Description != null) sb.AppendLine($"Description: {table.Description}");
            foreach (var col in table.Columns)
                sb.AppendLine($"  Column: {col.Name} ({col.DataType}){(col.IsKey ? " [PK]" : "")}{(col.IsForeignKey ? " [FK]" : "")}");

            docs.Add(new SchemaDocument
            {
                Id = $"{schemaId}-table-{table.Name}",
                SchemaId = schemaId.ToString(),
                TenantId = tenantId.ToString(),
                ChunkType = "table",
                Content = sb.ToString()
            });
        }

        if (context.Measures.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DAX Measures:");
            foreach (var m in context.Measures)
            {
                sb.AppendLine($"  {m.Name}");
                if (m.Expression != null) sb.AppendLine($"    = {m.Expression}");
            }

            docs.Add(new SchemaDocument
            {
                Id = $"{schemaId}-measures",
                SchemaId = schemaId.ToString(),
                TenantId = tenantId.ToString(),
                ChunkType = "measures",
                Content = sb.ToString()
            });
        }

        if (context.Relationships.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Relationships:");
            foreach (var r in context.Relationships)
                sb.AppendLine($"  {r.FromTable}[{r.FromColumn}] -> {r.ToTable}[{r.ToColumn}] ({r.Cardinality})");

            docs.Add(new SchemaDocument
            {
                Id = $"{schemaId}-relationships",
                SchemaId = schemaId.ToString(),
                TenantId = tenantId.ToString(),
                ChunkType = "relationships",
                Content = sb.ToString()
            });
        }

        return docs;
    }

    private static async Task EnsureIndexExistsAsync(SearchIndexClient client, CancellationToken ct)
    {
        try
        {
            await client.GetIndexAsync(IndexName, ct);
        }
        catch
        {
            var index = new SearchIndex(IndexName)
            {
                Fields =
                {
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                    new SimpleField("schemaId", SearchFieldDataType.String) { IsFilterable = true },
                    new SimpleField("tenantId", SearchFieldDataType.String) { IsFilterable = true },
                    new SimpleField("chunkType", SearchFieldDataType.String) { IsRetrievable = true },
                    new SearchableField("content") { IsRetrievable = true }
                }
            };
            await client.CreateIndexAsync(index, ct);
        }
    }
}

public class SchemaDocument
{
    public string Id { get; set; } = string.Empty;
    public string SchemaId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ChunkType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
