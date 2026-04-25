using QueryMind.Domain.Enums;
using QueryMind.Domain.Interfaces;

namespace QueryMind.Infrastructure.Parsers;

public class CsvHeadersParser : ISchemaParser
{
    public SchemaType SupportedType => SchemaType.CsvHeaders;

    public async Task<SchemaContext> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream);
        var headerLine = await reader.ReadLineAsync(ct) ?? string.Empty;
        var sampleLine = await reader.ReadLineAsync(ct);

        var headers = headerLine.Split(',').Select(h => h.Trim('"', '\'', ' ')).ToArray();
        var sampleValues = sampleLine?.Split(',') ?? [];

        var columns = headers.Select((h, i) =>
        {
            var sample = i < sampleValues.Length ? sampleValues[i].Trim('"', '\'', ' ') : null;
            var dataType = InferType(sample);
            return new ColumnDefinition(h, dataType, false, false, null);
        }).ToList();

        var tableName = Path.GetFileNameWithoutExtension(fileName);
        return new SchemaContext(
            tableName,
            SchemaType.CsvHeaders,
            [new TableDefinition(tableName, null, columns)],
            [],
            []
        );
    }

    private static string InferType(string? sample)
    {
        if (string.IsNullOrEmpty(sample)) return "string";
        if (int.TryParse(sample, out _)) return "integer";
        if (decimal.TryParse(sample, out _)) return "decimal";
        if (DateTime.TryParse(sample, out _)) return "datetime";
        if (bool.TryParse(sample, out _)) return "boolean";
        return "string";
    }
}
