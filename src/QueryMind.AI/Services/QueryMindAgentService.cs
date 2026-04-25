using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QueryMind.AI.Prompts;
using QueryMind.AI.Tools;
using QueryMind.Domain.Interfaces;

namespace QueryMind.AI.Services;

public partial class QueryMindAgentService(
    IConfiguration configuration,
    ISchemaSearchService searchService,
    ILogger<QueryMindAgentService> logger
) : IQueryMindAgent
{
    private const string ModelId = "claude-sonnet-4-20250514";
    private const int MaxTokens = 4096;
    private readonly string _apiKey = configuration["Anthropic:ApiKey"]
        ?? throw new InvalidOperationException("Anthropic:ApiKey is required");

    public async IAsyncEnumerable<AgentStreamEvent> GenerateQueryAsync(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var tools = new QueryMindTools(searchService);

        var schemaContext = request.SchemaContext
            ?? (request.SchemaId != Guid.Empty
                ? await searchService.SearchSchemaContextAsync(request.SchemaId, request.UserMessage, 6, ct)
                : null);

        var systemPrompt = SystemPromptBuilder.Build(request.PreferredDialect, schemaContext);

        var messages = BuildMessages(request);

        var payload = new
        {
            model = ModelId,
            max_tokens = MaxTokens,
            system = systemPrompt,
            tools = QueryMindTools.ToolDefinitions,
            stream = true,
            messages
        };

        var fullContent = new StringBuilder();
        var inputTokens = 0;
        var outputTokens = 0;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync("https://api.anthropic.com/v1/messages", content, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to call Anthropic API");
            yield return new AgentStreamEvent(AgentStreamEventType.Error, FullContent: ex.Message);
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? currentToolName = null;
        var toolInputBuilder = new StringBuilder();
        var toolCallPending = false;

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]") break;

            JsonElement evt;
            try { evt = JsonDocument.Parse(data).RootElement; }
            catch { continue; }

            var evtType = evt.TryGetProperty("type", out var t) ? t.GetString() : null;

            switch (evtType)
            {
                case "content_block_start":
                    if (evt.TryGetProperty("content_block", out var cb) &&
                        cb.TryGetProperty("type", out var cbType) && cbType.GetString() == "tool_use")
                    {
                        currentToolName = cb.TryGetProperty("name", out var tn) ? tn.GetString() : null;
                        toolInputBuilder.Clear();
                        toolCallPending = true;
                    }
                    break;

                case "content_block_delta":
                    if (!evt.TryGetProperty("delta", out var delta)) break;
                    var deltaType = delta.TryGetProperty("type", out var dt) ? dt.GetString() : null;

                    if (deltaType == "text_delta" && delta.TryGetProperty("text", out var textEl))
                    {
                        var text = textEl.GetString() ?? "";
                        fullContent.Append(text);
                        yield return new AgentStreamEvent(AgentStreamEventType.TextDelta, Delta: text);
                    }
                    else if (deltaType == "input_json_delta" && delta.TryGetProperty("partial_json", out var pj))
                    {
                        toolInputBuilder.Append(pj.GetString());
                    }
                    break;

                case "content_block_stop":
                    if (toolCallPending && currentToolName != null)
                    {
                        toolCallPending = false;
                        var toolInputJson = toolInputBuilder.ToString();
                        var schemaId = request.SchemaId != Guid.Empty ? request.SchemaId : (Guid?)null;

                        try
                        {
                            var toolInput = JsonDocument.Parse(toolInputJson.Length > 0 ? toolInputJson : "{}").RootElement;
                            var toolResult = await tools.ExecuteToolAsync(currentToolName, toolInput, schemaId, ct);

                            var toolMsg = $"\n[Schema Context Retrieved]\n{toolResult}\n";
                            fullContent.Append(toolMsg);
                            yield return new AgentStreamEvent(AgentStreamEventType.TextDelta, Delta: toolMsg);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Tool execution failed for {ToolName}", currentToolName);
                        }

                        currentToolName = null;
                    }
                    break;

                case "message_delta":
                    if (evt.TryGetProperty("usage", out var usage))
                    {
                        outputTokens = usage.TryGetProperty("output_tokens", out var ot) ? ot.GetInt32() : 0;
                    }
                    break;

                case "message_start":
                    if (evt.TryGetProperty("message", out var msg) && msg.TryGetProperty("usage", out var msgUsage))
                    {
                        inputTokens = msgUsage.TryGetProperty("input_tokens", out var it) ? it.GetInt32() : 0;
                    }
                    break;
            }
        }

        var finalContent = fullContent.ToString();
        var generatedQuery = ExtractQuery(finalContent);
        var explanation = ExtractExplanation(finalContent);

        yield return new AgentStreamEvent(
            AgentStreamEventType.Complete,
            FullContent: finalContent,
            GeneratedQuery: generatedQuery,
            Explanation: explanation,
            SchemaContextUsed: schemaContext,
            InputTokens: inputTokens,
            OutputTokens: outputTokens
        );
    }

    private static List<object> BuildMessages(AgentRequest request)
    {
        var messages = request.History
            .Select(h => (object)new { role = h.Role.ToString().ToLower(), content = h.Content })
            .ToList();

        messages.Add(new { role = "user", content = request.UserMessage });
        return messages;
    }

    private static string? ExtractQuery(string content)
    {
        var match = QueryBlockRegex().Match(content);
        return match.Success ? match.Groups[2].Value.Trim() : null;
    }

    private static string? ExtractExplanation(string content)
    {
        var withoutCode = QueryBlockRegex().Replace(content, "").Trim();
        var lines = withoutCode.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var explanationLines = lines.SkipWhile(l => l.StartsWith('#') || l.StartsWith("**")).Take(5);
        return string.Join(" ", explanationLines).Trim();
    }

    [GeneratedRegex(@"```(dax|sql|soql)?\s*\n(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex QueryBlockRegex();
}

internal static class AgentRequestExtensions
{
    public static Guid SchemaId(this AgentRequest r) =>
        r.SchemaContext != null ? Guid.Empty : Guid.Empty;
}

public static class AgentRequestEx
{
    public static Guid GetSchemaId(this AgentRequest r) => Guid.Empty;
}
