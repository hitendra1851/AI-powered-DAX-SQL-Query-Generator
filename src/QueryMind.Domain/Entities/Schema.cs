using QueryMind.Domain.Enums;

namespace QueryMind.Domain.Entities;

public class Schema
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public SchemaType Type { get; set; }
    public string? FileUrl { get; set; }
    public string? ParsedJson { get; set; }
    public string? EmbeddingId { get; set; }
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public bool IsProcessed { get; set; }
    public string? ProcessingError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<QuerySession> Sessions { get; set; } = new List<QuerySession>();
}
