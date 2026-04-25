using QueryMind.Domain.Enums;

namespace QueryMind.Domain.Interfaces;

public interface IQueryMindAgent
{
    IAsyncEnumerable<AgentStreamEvent> GenerateQueryAsync(
        AgentRequest request,
        CancellationToken ct = default);
}

public record AgentRequest(
    Guid SessionId,
    Guid TenantId,
    string UserMessage,
    QueryDialect PreferredDialect,
    IReadOnlyList<ConversationTurn> History,
    string? SchemaContext
);

public record ConversationTurn(MessageRole Role, string Content);

public record AgentStreamEvent(
    AgentStreamEventType Type,
    string? Delta = null,
    string? FullContent = null,
    string? GeneratedQuery = null,
    string? Explanation = null,
    string? SchemaContextUsed = null,
    int InputTokens = 0,
    int OutputTokens = 0
);

public enum AgentStreamEventType
{
    TextDelta,
    QueryExtracted,
    Complete,
    Error
}
