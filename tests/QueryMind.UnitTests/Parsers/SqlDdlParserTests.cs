using System.Text;
using FluentAssertions;
using QueryMind.Infrastructure.Parsers;
using Xunit;

namespace QueryMind.UnitTests.Parsers;

public class SqlDdlParserTests
{
    private readonly SqlDdlParser _parser = new();

    [Fact]
    public async Task Parse_BasicCreateTable_ExtractsTableAndColumns()
    {
        var ddl = """
                  CREATE TABLE [dbo].[Orders] (
                      [OrderId] INT NOT NULL PRIMARY KEY,
                      [CustomerId] INT NOT NULL,
                      [OrderDate] DATETIME NOT NULL,
                      [TotalAmount] DECIMAL(18,2) NOT NULL,
                      FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([CustomerId])
                  );
                  """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ddl));
        var result = await _parser.ParseAsync(stream, "test.sql");

        result.Tables.Should().HaveCount(1);
        result.Tables[0].Name.Should().Be("dbo].[Orders");
        result.Tables[0].Columns.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task Parse_MultipleCreateTables_ExtractsAll()
    {
        var ddl = """
                  CREATE TABLE Customers (
                      CustomerId INT NOT NULL,
                      Name VARCHAR(200) NOT NULL
                  );
                  CREATE TABLE Products (
                      ProductId INT NOT NULL,
                      ProductName VARCHAR(300) NOT NULL,
                      Price DECIMAL(10,2) NOT NULL
                  );
                  """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ddl));
        var result = await _parser.ParseAsync(stream, "schema.sql");

        result.Tables.Should().HaveCount(2);
        result.Tables.Select(t => t.Name).Should().Contain("Customers");
        result.Tables.Select(t => t.Name).Should().Contain("Products");
    }

    [Fact]
    public async Task Parse_ForeignKey_ExtractsRelationship()
    {
        var ddl = """
                  CREATE TABLE Orders (
                      OrderId INT PRIMARY KEY,
                      CustomerId INT,
                      FOREIGN KEY (CustomerId) REFERENCES Customers(CustomerId)
                  );
                  """;

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ddl));
        var result = await _parser.ParseAsync(stream, "orders.sql");

        result.Relationships.Should().HaveCount(1);
        result.Relationships[0].FromColumn.Should().Be("CustomerId");
        result.Relationships[0].ToTable.Should().Be("Customers");
    }
}
