namespace QueryMind.Domain.Entities;

public class QueryFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public QueryMessage Message { get; set; } = null!;
}
