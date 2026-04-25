using QueryMind.Domain.Enums;

namespace QueryMind.Domain.Entities;

public class QuerySession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? SchemaId { get; set; }
    public QueryDialect DefaultDialect { get; set; } = QueryDialect.Dax;
    public string? Title { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
    public Schema? Schema { get; set; }
    public ICollection<QueryMessage> Messages { get; set; } = new List<QueryMessage>();
}
