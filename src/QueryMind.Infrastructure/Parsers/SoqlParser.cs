using System.Text.Json;
using QueryMind.Domain.Enums;
using QueryMind.Domain.Interfaces;

namespace QueryMind.Infrastructure.Parsers;

/// <summary>
/// Parses Salesforce Describe API JSON exports into a SchemaContext.
/// </summary>
public class SoqlParser : ISchemaParser
{
    public SchemaType SupportedType => SchemaType.Soql;

    public async Task<SchemaContext> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var doc = await JsonDocument.ParseAsync(fileStream, cancellationToken: ct);
        var root = doc.RootElement;

        var tables = new List<TableDefinition>();

        // Support single object or array of objects
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var obj in root.EnumerateArray())
                tables.Add(ParseSalesforceObject(obj));
        }
        else if (root.TryGetProperty("sobjects", out var sobjects))
        {
            foreach (var obj in sobjects.EnumerateArray())
                tables.Add(ParseSalesforceObject(obj));
        }
        else
        {
            tables.Add(ParseSalesforceObject(root));
        }

        return new SchemaContext(
            Path.GetFileNameWithoutExtension(fileName),
            SchemaType.Soql,
            tables, [], []
        );
    }

    private static TableDefinition ParseSalesforceObject(JsonElement obj)
    {
        var name = obj.TryGetProperty("name", out var n) ? n.GetString() ?? "Object" : "Object";
        var label = obj.TryGetProperty("label", out var l) ? l.GetString() : null;
        var columns = new List<ColumnDefinition>();

        if (obj.TryGetProperty("fields", out var fields))
        {
            foreach (var field in fields.EnumerateArray())
            {
                var fieldName = field.TryGetProperty("name", out var fn) ? fn.GetString() ?? "Field" : "Field";
                var fieldType = field.TryGetProperty("type", out var ft) ? ft.GetString() ?? "string" : "string";
                var isId = fieldName == "Id";
                var isRef = fieldType == "reference";
                var fieldLabel = field.TryGetProperty("label", out var fl) ? fl.GetString() : null;

                columns.Add(new ColumnDefinition(fieldName, fieldType, isId, isRef, fieldLabel));
            }
        }

        return new TableDefinition(name, label, columns);
    }
}
