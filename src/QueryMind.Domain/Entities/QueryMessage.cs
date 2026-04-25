using QueryMind.Domain.Enums;

namespace QueryMind.Domain.Entities;

public class QueryMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? GeneratedQuery { get; set; }
    public QueryDialect? Dialect { get; set; }
    public string? Explanation { get; set; }
    public string? SchemaContextUsed { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int LatencyMs { get; set; }
    public string? ModelVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public QuerySession Session { get; set; } = null!;
    public QueryFeedback? Feedback { get; set; }
}
