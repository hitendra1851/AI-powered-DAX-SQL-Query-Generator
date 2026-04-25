using QueryMind.Domain.Enums;
using QueryMind.Domain.Interfaces;

namespace QueryMind.Infrastructure.Services;

public class SchemaParserFactory(IEnumerable<ISchemaParser> parsers)
{
    private readonly Dictionary<SchemaType, ISchemaParser> _parsers =
        parsers.ToDictionary(p => p.SupportedType);

    public ISchemaParser GetParser(SchemaType type)
    {
        if (!_parsers.TryGetValue(type, out var parser))
            throw new NotSupportedException($"No parser registered for schema type: {type}");
        return parser;
    }

    public SchemaType DetectType(string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".json" when fileName.Contains("bim", StringComparison.OrdinalIgnoreCase) => SchemaType.TabularBim,
            ".json" when fileName.Contains("fabric", StringComparison.OrdinalIgnoreCase) => SchemaType.FabricLakehouse,
            ".json" when fileName.Contains("salesforce", StringComparison.OrdinalIgnoreCase) => SchemaType.Soql,
            ".json" => SchemaType.PbixJson,
            ".sql" => SchemaType.SqlDdl,
            ".ddl" => SchemaType.SqlDdl,
            ".csv" => SchemaType.CsvHeaders,
            _ => SchemaType.PbixJson
        };
    }
}
