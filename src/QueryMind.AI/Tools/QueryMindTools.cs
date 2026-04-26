using System.Text.Json;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime.Documents;
using QueryMind.Domain.Interfaces;

namespace QueryMind.AI.Tools;

public class QueryMindTools(ISchemaSearchService searchService)
{
    // Bedrock Converse API tool definitions (Amazon.BedrockRuntime.Model.Tool)
    public static readonly List<Tool> BedrockToolDefinitions =
    [
        new Tool
        {
            ToolSpec = new ToolSpecification
            {
                Name = "get_schema_context",
                Description = "Retrieve relevant schema information (tables, columns, measures, relationships) " +
                              "based on the user query. Call this before generating a query to ensure accuracy.",
                InputSchema = new ToolInputSchema
                {
                    Json = BuildDocument(new Dictionary<string, Document>
                    {
                        ["type"] = "object",
                        ["properties"] = BuildDocument(new Dictionary<string, Document>
                        {
                            ["query"] = BuildDocument(new Dictionary<string, Document>
                            {
                                ["type"] = "string",
                                ["description"] = "Keywords to search for in the schema"
                            }),
                            ["schema_id"] = BuildDocument(new Dictionary<string, Document>
                            {
                                ["type"] = "string",
                                ["description"] = "UUID of the schema to search"
                            })
                        }),
                        ["required"] = BuildList(["query", "schema_id"])
                    })
                }
            }
        },
        new Tool
        {
            ToolSpec = new ToolSpecification
            {
                Name = "validate_dax_syntax",
                Description = "Check a DAX expression for common performance and correctness issues.",
                InputSchema = new ToolInputSchema
                {
                    Json = BuildDocument(new Dictionary<string, Document>
                    {
                        ["type"] = "object",
                        ["properties"] = BuildDocument(new Dictionary<string, Document>
                        {
                            ["dax"] = BuildDocument(new Dictionary<string, Document>
                            {
                                ["type"] = "string",
                                ["description"] = "The DAX expression to validate"
                            })
                        }),
                        ["required"] = BuildList(["dax"])
                    })
                }
            }
        },
        new Tool
        {
            ToolSpec = new ToolSpecification
            {
                Name = "get_query_examples",
                Description = "Return example queries for a specific pattern (e.g. time_intelligence_ytd, running_total, rank_top_n).",
                InputSchema = new ToolInputSchema
                {
                    Json = BuildDocument(new Dictionary<string, Document>
                    {
                        ["type"] = "object",
                        ["properties"] = BuildDocument(new Dictionary<string, Document>
                        {
                            ["query_type"] = BuildDocument(new Dictionary<string, Document>
                            {
                                ["type"] = "string",
                                ["description"] = "Pattern name: time_intelligence_ytd | running_total | rank_top_n | period_over_period"
                            }),
                            ["dialect"] = BuildDocument(new Dictionary<string, Document>
                            {
                                ["type"] = "string",
                                ["description"] = "dax | sql | soql"
                            })
                        }),
                        ["required"] = BuildList(["query_type", "dialect"])
                    })
                }
            }
        }
    ];

    public async Task<string> ExecuteToolAsync(
        string toolName,
        JsonElement input,
        Guid? schemaId,
        CancellationToken ct = default)
    {
        return toolName switch
        {
            "get_schema_context" => await GetSchemaContextAsync(input, schemaId, ct),
            "validate_dax_syntax" => ValidateDaxSyntax(input),
            "get_query_examples" => GetQueryExamples(input),
            _ => $"Unknown tool: {toolName}"
        };
    }

    private async Task<string> GetSchemaContextAsync(JsonElement input, Guid? schemaId, CancellationToken ct)
    {
        var query = input.GetProperty("query").GetString() ?? string.Empty;
        var idStr = input.TryGetProperty("schema_id", out var sid) ? sid.GetString() : null;
        var targetId = idStr != null && Guid.TryParse(idStr, out var pid) ? pid : schemaId;

        if (targetId == null) return "No schema loaded. Upload a schema file first.";

        return await searchService.SearchSchemaContextAsync(targetId.Value, query, topK: 5, ct);
    }

    private static string ValidateDaxSyntax(JsonElement input)
    {
        var dax = input.GetProperty("dax").GetString() ?? string.Empty;
        var issues = new List<string>();

        if (dax.Contains('/') && !dax.Contains("DIVIDE", StringComparison.OrdinalIgnoreCase))
            issues.Add("Use DIVIDE() instead of / to handle division-by-zero safely.");

        if (dax.Contains("DISTINCTCOUNT", StringComparison.OrdinalIgnoreCase))
            issues.Add("DISTINCTCOUNT on large columns is slow — consider SUMMARIZE + COUNTROWS.");

        if (dax.Contains("ALL(", StringComparison.OrdinalIgnoreCase) &&
            dax.Contains("CALCULATE", StringComparison.OrdinalIgnoreCase))
            issues.Add("ALL() inside CALCULATE removes all filters — use ALLEXCEPT if partial filter removal is intended.");

        return issues.Count == 0
            ? "No common issues found."
            : "DAX issues:\n" + string.Join("\n", issues.Select(i => $"- {i}"));
    }

    private static string GetQueryExamples(JsonElement input)
    {
        var queryType = input.GetProperty("query_type").GetString()?.ToLower() ?? "";
        var dialect = input.TryGetProperty("dialect", out var d) ? d.GetString()?.ToLower() : "dax";

        return (queryType, dialect) switch
        {
            ("time_intelligence_ytd", "dax") =>
                "```dax\nYTD Sales = CALCULATE([Total Sales], DATESYTD('Date'[Date]))\n```\nRequires a marked date table.",
            ("running_total", "dax") =>
                "```dax\nRunning Total = CALCULATE([Total Sales], FILTER(ALL('Date'), 'Date'[Date] <= MAX('Date'[Date])))\n```",
            ("rank_top_n", "dax") =>
                "```dax\nProduct Rank = RANKX(ALL('Product'), [Total Sales])\nTop 10 Flag = IF([Product Rank] <= 10, \"Top 10\", \"Other\")\n```",
            ("period_over_period", "dax") =>
                "```dax\nSales PY = CALCULATE([Total Sales], SAMEPERIODLASTYEAR('Date'[Date]))\nYoY% = DIVIDE([Total Sales] - [Sales PY], [Sales PY])\n```",
            ("running_total", "sql") =>
                "```sql\nSELECT order_date, SUM(amount) OVER (ORDER BY order_date ROWS UNBOUNDED PRECEDING) AS running_total\nFROM orders\n```",
            ("rank_top_n", "sql") =>
                "```sql\nWITH ranked AS (\n  SELECT product_id, SUM(revenue) total, DENSE_RANK() OVER (ORDER BY SUM(revenue) DESC) rnk\n  FROM sales GROUP BY product_id\n)\nSELECT * FROM ranked WHERE rnk <= 10\n```",
            _ => $"No canned example for '{queryType}' in {dialect}. Describe your goal and I will generate from your schema."
        };
    }

    // Helpers to build Amazon.Runtime.Documents.Document from primitives
    private static Document BuildDocument(Dictionary<string, Document> dict) => new(dict);
    private static Document BuildList(IEnumerable<string> items) =>
        new(items.Select(s => new Document(s)).ToList());
}
