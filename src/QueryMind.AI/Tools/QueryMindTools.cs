using System.Text.Json;
using QueryMind.Domain.Interfaces;

namespace QueryMind.AI.Tools;

public class QueryMindTools(ISchemaSearchService searchService)
{
    public static readonly IReadOnlyList<object> ToolDefinitions =
    [
        new
        {
            name = "get_schema_context",
            description = "Retrieve relevant schema information (tables, columns, measures, relationships) based on the user's query. Always call this before generating a query to ensure you have accurate schema details.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "The natural language query or keywords to search for in the schema" },
                    schema_id = new { type = "string", description = "The UUID of the schema to search in" }
                },
                required = new[] { "query", "schema_id" }
            }
        },
        new
        {
            name = "validate_dax_syntax",
            description = "Validate DAX syntax for common errors. Returns a list of issues found.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    dax = new { type = "string", description = "The DAX expression to validate" }
                },
                required = new[] { "dax" }
            }
        },
        new
        {
            name = "get_query_examples",
            description = "Get example queries for a specific query type or pattern.",
            input_schema = new
            {
                type = "object",
                properties = new
                {
                    query_type = new { type = "string", description = "Type of query pattern, e.g. 'time_intelligence_ytd', 'running_total', 'rank_top_n', 'period_over_period'" },
                    dialect = new { type = "string", description = "Query dialect: 'dax', 'sql', 'soql'" }
                },
                required = new[] { "query_type", "dialect" }
            }
        }
    ];

    public async Task<string> ExecuteToolAsync(string toolName, JsonElement input, Guid? schemaId, CancellationToken ct = default)
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

        var targetId = idStr != null && Guid.TryParse(idStr, out var parsedId) ? parsedId : schemaId;
        if (targetId == null) return "No schema loaded. Please upload a schema file first.";

        return await searchService.SearchSchemaContextAsync(targetId.Value, query, topK: 6, ct);
    }

    private static string ValidateDaxSyntax(JsonElement input)
    {
        var dax = input.GetProperty("dax").GetString() ?? string.Empty;
        var issues = new List<string>();

        if (dax.Contains('/') && !dax.Contains("DIVIDE", StringComparison.OrdinalIgnoreCase))
            issues.Add("Consider using DIVIDE() instead of / to handle division-by-zero safely.");

        if (dax.Contains("DISTINCTCOUNT", StringComparison.OrdinalIgnoreCase))
            issues.Add("DISTINCTCOUNT on large columns can be slow. Consider using SUMMARIZE + COUNTROWS for better performance.");

        if (dax.Contains("ALL(", StringComparison.OrdinalIgnoreCase) && dax.Contains("CALCULATE", StringComparison.OrdinalIgnoreCase))
            issues.Add("ALL() inside CALCULATE removes all filters. Ensure this is intentional — consider ALLEXCEPT if you want to preserve some filters.");

        if (!issues.Any())
            return "DAX syntax looks good. No common performance or correctness issues detected.";

        return "DAX Validation Results:\n" + string.Join("\n", issues.Select(i => $"- {i}"));
    }

    private static string GetQueryExamples(JsonElement input)
    {
        var queryType = input.GetProperty("query_type").GetString()?.ToLower() ?? "";
        var dialect = input.TryGetProperty("dialect", out var d) ? d.GetString()?.ToLower() : "dax";

        return (queryType, dialect) switch
        {
            ("time_intelligence_ytd", "dax") => """
                Example: Year-to-Date Sales
                ```dax
                YTD Sales = CALCULATE([Total Sales], DATESYTD('Date'[Date]))
                ```
                Use DATESYTD with a date column from your date table. Ensure your model has a marked date table.
                """,
            ("running_total", "dax") => """
                Example: Running Total
                ```dax
                Running Total = CALCULATE([Total Sales], FILTER(ALL('Date'), 'Date'[Date] <= MAX('Date'[Date])))
                ```
                """,
            ("rank_top_n", "dax") => """
                Example: Top N ranking
                ```dax
                Product Rank = RANKX(ALL('Product'), [Total Sales])
                Top 10 Flag = IF([Product Rank] <= 10, "Top 10", "Other")
                ```
                """,
            ("period_over_period", "dax") => """
                Example: Month-over-Month change
                ```dax
                MoM Change % = DIVIDE([Total Sales] - [Sales PY Month], [Sales PY Month])
                Sales PY Month = CALCULATE([Total Sales], DATEADD('Date'[Date], -1, MONTH))
                ```
                """,
            ("running_total", "sql") => """
                Example: Running total with window function
                ```sql
                SELECT
                    order_date,
                    amount,
                    SUM(amount) OVER (ORDER BY order_date ROWS UNBOUNDED PRECEDING) AS running_total
                FROM orders
                ```
                """,
            ("rank_top_n", "sql") => """
                Example: Top N with DENSE_RANK
                ```sql
                WITH ranked AS (
                    SELECT product_id, SUM(revenue) AS total_revenue,
                           DENSE_RANK() OVER (ORDER BY SUM(revenue) DESC) AS rnk
                    FROM sales GROUP BY product_id
                )
                SELECT * FROM ranked WHERE rnk <= 10
                ```
                """,
            _ => $"No specific example found for '{queryType}' in {dialect}. Please describe your query pattern and I will generate one from your schema."
        };
    }
}
