using System.Text.Json;
using QueryMind.Domain.Enums;
using QueryMind.Domain.Interfaces;

namespace QueryMind.Infrastructure.Parsers;

public class FabricLakehouseParser : ISchemaParser
{
    public SchemaType SupportedType => SchemaType.FabricLakehouse;

    public async Task<SchemaContext> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var doc = await JsonDocument.ParseAsync(fileStream, cancellationToken: ct);
        var root = doc.RootElement;

        var tables = new List<TableDefinition>();

        // Support both single-table and multi-table Fabric exports
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var tableEl in root.EnumerateArray())
                tables.Add(ParseFabricTable(tableEl));
        }
        else if (root.TryGetProperty("tables", out var tablesEl))
        {
            foreach (var tableEl in tablesEl.EnumerateArray())
                tables.Add(ParseFabricTable(tableEl));
        }
        else
        {
            tables.Add(ParseFabricTable(root));
        }

        return new SchemaContext(
            Path.GetFileNameWithoutExtension(fileName),
            SchemaType.FabricLakehouse,
            tables, [], []
        );
    }

    private static TableDefinition ParseFabricTable(JsonElement tableEl)
    {
        var tableName = tableEl.TryGetProperty("name", out var tn) ? tn.GetString() ?? "Table" : "Table";
        var columns = new List<ColumnDefinition>();

        var schemaEl = tableEl.TryGetProperty("schema", out var s) ? s
                     : tableEl.TryGetProperty("fields", out var f) ? f
                     : tableEl.TryGetProperty("columns", out var c) ? c
                     : default;

        if (schemaEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var col in schemaEl.EnumerateArray())
            {
                var colName = (col.TryGetProperty("name", out var cn) ? cn.GetString() : null)
                           ?? (col.TryGetProperty("columnName", out var cn2) ? cn2.GetString() : null)
                           ?? "Column";

                var dataType = (col.TryGetProperty("type", out var dt) ? dt.GetString() : null)
                            ?? (col.TryGetProperty("dataType", out var dt2) ? dt2.GetString() : null)
                            ?? "string";

                var isPartition = col.TryGetProperty("isPartitionField", out var ip) && ip.GetBoolean();
                columns.Add(new ColumnDefinition(colName, dataType, isPartition, false, null));
            }
        }

        // Bronze/Silver/Gold layer hint
        var layer = tableName.StartsWith("Bronze_", StringComparison.OrdinalIgnoreCase) ? "Bronze"
                  : tableName.StartsWith("Silver_", StringComparison.OrdinalIgnoreCase) ? "Silver"
                  : tableName.StartsWith("Gold_", StringComparison.OrdinalIgnoreCase) ? "Gold"
                  : null;

        return new TableDefinition(tableName, layer != null ? $"Fabric Lakehouse {layer} layer" : null, columns);
    }
}
