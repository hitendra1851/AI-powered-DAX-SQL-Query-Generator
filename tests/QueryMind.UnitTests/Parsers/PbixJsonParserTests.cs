using System.Text;
using System.Text.Json;
using FluentAssertions;
using QueryMind.Infrastructure.Parsers;
using Xunit;

namespace QueryMind.UnitTests.Parsers;

public class PbixJsonParserTests
{
    private readonly PbixJsonParser _parser = new();

    [Fact]
    public async Task Parse_ValidPbixJson_ExtractsTablesAndMeasures()
    {
        var pbixJson = new
        {
            model = new
            {
                tables = new[]
                {
                    new
                    {
                        name = "Sales",
                        columns = new[]
                        {
                            new { name = "SalesId", dataType = "int64", isKey = true },
                            new { name = "Amount", dataType = "decimal" }
                        },
                        measures = new[]
                        {
                            new { name = "Total Sales", expression = "SUM(Sales[Amount])", formatString = "#,##0.00" }
                        }
                    },
                    new
                    {
                        name = "Date",
                        columns = new[]
                        {
                            new { name = "Date", dataType = "dateTime", isKey = false },
                            new { name = "Year", dataType = "int64" }
                        },
                        measures = Array.Empty<object>()
                    }
                },
                relationships = new[]
                {
                    new { fromTable = "Sales", fromColumn = "DateKey", toTable = "Date", toColumn = "DateKey", crossFilteringBehavior = "OneToMany" }
                }
            }
        };

        var json = JsonSerializer.Serialize(pbixJson);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = await _parser.ParseAsync(stream, "model.json");

        result.Tables.Should().HaveCount(2);
        result.Measures.Should().HaveCount(1);
        result.Measures[0].Name.Should().Be("Total Sales");
        result.Relationships.Should().HaveCount(1);
    }

    [Fact]
    public async Task Parse_FiltersOutSystemTables()
    {
        var pbixJson = new
        {
            model = new
            {
                tables = new[]
                {
                    new { name = "Sales", columns = Array.Empty<object>(), measures = Array.Empty<object>() },
                    new { name = "DateTableTemplate_abc", columns = Array.Empty<object>(), measures = Array.Empty<object>() },
                    new { name = "LocalDateTable_xyz", columns = Array.Empty<object>(), measures = Array.Empty<object>() }
                },
                relationships = Array.Empty<object>()
            }
        };

        var json = JsonSerializer.Serialize(pbixJson);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var result = await _parser.ParseAsync(stream, "model.json");

        result.Tables.Should().HaveCount(1);
        result.Tables[0].Name.Should().Be("Sales");
    }
}
