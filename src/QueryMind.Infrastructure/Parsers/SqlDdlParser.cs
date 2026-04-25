using System.Text.RegularExpressions;
using QueryMind.Domain.Enums;
using QueryMind.Domain.Interfaces;

namespace QueryMind.Infrastructure.Parsers;

public partial class SqlDdlParser : ISchemaParser
{
    public SchemaType SupportedType => SchemaType.SqlDdl;

    public async Task<SchemaContext> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var reader = new StreamReader(fileStream);
        var ddl = await reader.ReadToEndAsync(ct);

        var tables = ParseTables(ddl);
        var relationships = ParseForeignKeys(ddl);

        return new SchemaContext(
            Path.GetFileNameWithoutExtension(fileName),
            SchemaType.SqlDdl,
            tables,
            [],
            relationships
        );
    }

    private static List<TableDefinition> ParseTables(string ddl)
    {
        var tables = new List<TableDefinition>();
        var tablePattern = CreateTableRegex();

        foreach (Match tableMatch in tablePattern.Matches(ddl))
        {
            var tableName = tableMatch.Groups[1].Value.Trim('[', ']', '`', '"');
            var body = tableMatch.Groups[2].Value;
            var columns = ParseColumns(body);
            tables.Add(new TableDefinition(tableName, null, columns));
        }

        return tables;
    }

    private static List<ColumnDefinition> ParseColumns(string body)
    {
        var columns = new List<ColumnDefinition>();
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim().TrimEnd(',');
            if (trimmed.StartsWith("--") || trimmed.Length == 0) continue;
            if (Regex.IsMatch(trimmed, @"^\s*(PRIMARY|FOREIGN|UNIQUE|CHECK|CONSTRAINT|INDEX)", RegexOptions.IgnoreCase))
                continue;

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            var colName = parts[0].Trim('[', ']', '`', '"');
            var dataType = parts[1];
            var isPk = trimmed.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase);
            var isFk = trimmed.Contains("REFERENCES", StringComparison.OrdinalIgnoreCase);

            columns.Add(new ColumnDefinition(colName, dataType, isPk, isFk, null));
        }

        return columns;
    }

    private static List<RelationshipDefinition> ParseForeignKeys(string ddl)
    {
        var rels = new List<RelationshipDefinition>();
        var fkPattern = ForeignKeyRegex();

        foreach (Match m in fkPattern.Matches(ddl))
        {
            rels.Add(new RelationshipDefinition(
                FromTable: "CurrentTable",
                FromColumn: m.Groups[1].Value.Trim('[', ']', '`', '"'),
                ToTable: m.Groups[2].Value.Trim('[', ']', '`', '"'),
                ToColumn: m.Groups[3].Value.Trim('[', ']', '`', '"'),
                Cardinality: "ManyToOne"
            ));
        }

        return rels;
    }

    [GeneratedRegex(@"CREATE\s+TABLE\s+(?:\w+\.)?([^\s(]+)\s*\(([^;]+)\)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CreateTableRegex();

    [GeneratedRegex(@"FOREIGN\s+KEY\s*\(([^)]+)\)\s+REFERENCES\s+([^\s(]+)\s*\(([^)]+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex ForeignKeyRegex();
}
