using System.Text;
using FluentAssertions;
using QueryMind.Infrastructure.Parsers;
using Xunit;

namespace QueryMind.UnitTests.Parsers;

public class CsvHeadersParserTests
{
    private readonly CsvHeadersParser _parser = new();

    [Fact]
    public async Task Parse_WithHeaders_ExtractsColumns()
    {
        var csv = "patient_id,admission_date,diagnosis_code,total_cost\n1001,2024-01-15,E11.9,1250.00";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await _parser.ParseAsync(stream, "patients.csv");

        result.Tables.Should().HaveCount(1);
        result.Tables[0].Name.Should().Be("patients");
        result.Tables[0].Columns.Should().HaveCount(4);
        result.Tables[0].Columns.Select(c => c.Name).Should().Contain("patient_id");
    }

    [Fact]
    public async Task Parse_InfersNumericType()
    {
        var csv = "id,amount,is_active\n1,99.99,true";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await _parser.ParseAsync(stream, "data.csv");

        var amountCol = result.Tables[0].Columns.First(c => c.Name == "amount");
        amountCol.DataType.Should().Be("decimal");

        var idCol = result.Tables[0].Columns.First(c => c.Name == "id");
        idCol.DataType.Should().Be("integer");
    }
}
