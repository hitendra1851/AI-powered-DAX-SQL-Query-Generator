using QueryMind.Domain.Enums;

namespace QueryMind.Domain.Entities;

public class QueryTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public QueryDialect Dialect { get; set; }
    public string QueryText { get; set; } = string.Empty;
    public string NaturalLanguagePrompt { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
