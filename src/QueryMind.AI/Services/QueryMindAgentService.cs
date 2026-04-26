using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
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
    private readonly string _modelId = configuration["AWS:BedrockModelId"]
        ?? "arn:aws:bedrock:us-east-1:183631304469:inference-profile/us.anthropic.claude-opus-4-7";

    private readonly string _region = configuration["AWS:Region"] ?? "us-east-1";

    private const int MaxTokens = 32000;

    public async IAsyncEnumerable<AgentStreamEvent> GenerateQueryAsync(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var schemaContext = request.SchemaContext;
        var systemPrompt = SystemPromptBuilder.Build(request.PreferredDialect, schemaContext);

        // IAM credentials come from the environment:
        //   - App Runner: instance role attached in CDK
        //   - Local dev:  AWS_PROFILE / AWS_ACCESS_KEY_ID + AWS_SECRET_ACCESS_KEY
        var client = new AmazonBedrockRuntimeClient(RegionEndpoint.GetBySystemName(_region));

        var converseRequest = new ConverseStreamRequest
        {
            ModelId = _modelId,
            System = [new SystemContentBlock { Text = systemPrompt }],
            Messages = BuildBedrockMessages(request),
            InferenceConfig = new InferenceConfiguration
            {
                MaxTokens = MaxTokens
            },
            ToolConfig = new ToolConfiguration
            {
                Tools = QueryMindTools.BedrockToolDefinitions,
                ToolChoice = new ToolChoice { Auto = new AutoToolChoice() }
            },
            PerformanceConfig = new PerformanceConfiguration
            {
                Latency = PerformanceConfigLatency.Standard
            }
        };

        // Bridge event-based AWS SDK stream → IAsyncEnumerable via a Channel
        var channel = Channel.CreateUnbounded<AgentStreamEvent>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        var streamTask = ProcessStreamAsync(client, converseRequest, searchService, channel.Writer, logger, ct);

        await foreach (var evt in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            yield return evt;

        // Propagate any exception from the stream task
        await streamTask.ConfigureAwait(false);
    }

    private static async Task ProcessStreamAsync(
        AmazonBedrockRuntimeClient client,
        ConverseStreamRequest request,
        ISchemaSearchService searchService,
        ChannelWriter<AgentStreamEvent> writer,
        ILogger logger,
        CancellationToken ct)
    {
        var tools = new QueryMindTools(searchService);
        var fullContent = new StringBuilder();
        var toolInputBuilder = new StringBuilder();
        string? currentToolName = null;
        string? currentToolUseId = null;
        var inputTokens = 0;
        var outputTokens = 0;

        try
        {
            var response = await client.ConverseStreamAsync(request, ct).ConfigureAwait(false);

            await foreach (var streamEvent in response.Stream.ConfigureAwait(false))
            {
                switch (streamEvent)
                {
                    case ContentBlockDeltaEvent delta:
                        if (delta.Delta?.Text is { Length: > 0 } text)
                        {
                            fullContent.Append(text);
                            await writer.WriteAsync(
                                new AgentStreamEvent(AgentStreamEventType.TextDelta, Delta: text), ct);
                        }
                        else if (delta.Delta?.ToolUse?.Input is { Length: > 0 } toolInput)
                        {
                            toolInputBuilder.Append(toolInput);
                        }
                        break;

                    case ContentBlockStartEvent start:
                        if (start.ContentBlock?.ToolUse is { } toolUseBlock)
                        {
                            currentToolName = toolUseBlock.Name;
                            currentToolUseId = toolUseBlock.ToolUseId;
                            toolInputBuilder.Clear();
                        }
                        break;

                    case ContentBlockStopEvent:
                        if (currentToolName != null)
                        {
                            try
                            {
                                var toolJson = toolInputBuilder.ToString();
                                var toolInput = JsonDocument.Parse(toolJson.Length > 0 ? toolJson : "{}").RootElement;
                                var toolResult = await tools.ExecuteToolAsync(currentToolName, toolInput, null, ct)
                                    .ConfigureAwait(false);

                                var toolMsg = $"\n[{currentToolName}]\n{toolResult}\n";
                                fullContent.Append(toolMsg);
                                await writer.WriteAsync(
                                    new AgentStreamEvent(AgentStreamEventType.TextDelta, Delta: toolMsg), ct);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Tool {Tool} execution failed", currentToolName);
                            }

                            currentToolName = null;
                            currentToolUseId = null;
                        }
                        break;

                    case MessageDeltaEvent msgDelta:
                        inputTokens = msgDelta.Usage?.InputTokens ?? inputTokens;
                        outputTokens = msgDelta.Usage?.OutputTokens ?? outputTokens;
                        break;
                }
            }

            var final = fullContent.ToString();
            await writer.WriteAsync(new AgentStreamEvent(
                AgentStreamEventType.Complete,
                FullContent: final,
                GeneratedQuery: ExtractQuery(final),
                Explanation: ExtractExplanation(final),
                SchemaContextUsed: request.System.FirstOrDefault()?.Text,
                InputTokens: inputTokens,
                OutputTokens: outputTokens
            ), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bedrock ConverseStream failed");
            await writer.WriteAsync(
                new AgentStreamEvent(AgentStreamEventType.Error, FullContent: ex.Message), ct);
        }
        finally
        {
            writer.Complete();
        }
    }

    private static List<Message> BuildBedrockMessages(AgentRequest request)
    {
        var messages = request.History
            .Select(h => new Message
            {
                Role = h.Role == MessageRole.User
                    ? ConversationRole.User
                    : ConversationRole.Assistant,
                Content = [new ContentBlock { Text = h.Content }]
            })
            .ToList();

        messages.Add(new Message
        {
            Role = ConversationRole.User,
            Content = [new ContentBlock { Text = request.UserMessage }]
        });

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
        return string.Join(" ", lines.SkipWhile(l => l.StartsWith('#') || l.StartsWith("**")).Take(5)).Trim();
    }

    [GeneratedRegex(@"```(dax|sql|soql)?\s*\n(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex QueryBlockRegex();
}
