using System.Text.Json;
using QueryMind.Domain.Enums;
using QueryMind.Domain.Interfaces;

namespace QueryMind.Infrastructure.Parsers;

public class TabularBimParser : ISchemaParser
{
    public SchemaType SupportedType => SchemaType.TabularBim;

    public async Task<SchemaContext> ParseAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var doc = await JsonDocument.ParseAsync(fileStream, cancellationToken: ct);
        var root = doc.RootElement;

        var tables = new List<TableDefinition>();
        var measures = new List<MeasureDefinition>();
        var relationships = new List<RelationshipDefinition>();

        var model = root.TryGetProperty("model", out var m) ? m
                  : root.TryGetProperty("Model", out var m2) ? m2
                  : root;

        if (model.TryGetProperty("tables", out var tablesEl))
        {
            foreach (var table in tablesEl.EnumerateArray())
            {
                var tableName = table.GetProperty("name").GetString() ?? "Unknown";
                var columns = new List<ColumnDefinition>();

                if (table.TryGetProperty("columns", out var cols))
                {
                    foreach (var col in cols.EnumerateArray())
                    {
                        columns.Add(new ColumnDefinition(
                            Name: col.GetProperty("name").GetString() ?? "Column",
                            DataType: col.TryGetProperty("dataType", out var dt) ? dt.GetString() ?? "string" : "string",
                            IsKey: col.TryGetProperty("isKey", out var ik) && ik.GetBoolean(),
                            IsForeignKey: false,
                            Description: col.TryGetProperty("description", out var d) ? d.GetString() : null
                        ));
                    }
                }

                if (table.TryGetProperty("measures", out var meaEl))
                {
                    foreach (var mea in meaEl.EnumerateArray())
                    {
                        measures.Add(new MeasureDefinition(
                            Name: mea.GetProperty("name").GetString() ?? "Measure",
                            Expression: mea.TryGetProperty("expression", out var e) ? e.GetString() : null,
                            FormatString: mea.TryGetProperty("formatString", out var fs) ? fs.GetString() : null,
                            Description: mea.TryGetProperty("description", out var desc) ? desc.GetString() : null
                        ));
                    }
                }

                tables.Add(new TableDefinition(tableName, null, columns));
            }
        }

        if (model.TryGetProperty("relationships", out var rels))
        {
            foreach (var rel in rels.EnumerateArray())
            {
                relationships.Add(new RelationshipDefinition(
                    FromTable: rel.TryGetProperty("fromTable", out var ft) ? ft.GetString() ?? "" : "",
                    FromColumn: rel.TryGetProperty("fromColumn", out var fc) ? fc.GetString() ?? "" : "",
                    ToTable: rel.TryGetProperty("toTable", out var tt) ? tt.GetString() ?? "" : "",
                    ToColumn: rel.TryGetProperty("toColumn", out var tc) ? tc.GetString() ?? "" : "",
                    Cardinality: rel.TryGetProperty("joinOnDateBehavior", out var jdb) ? jdb.GetString() ?? "OneToMany" : "OneToMany"
                ));
            }
        }

        return new SchemaContext(
            Path.GetFileNameWithoutExtension(fileName),
            SchemaType.TabularBim,
            tables, measures, relationships
        );
    }
}
