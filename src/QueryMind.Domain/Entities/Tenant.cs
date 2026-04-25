using QueryMind.Domain.Enums;

namespace QueryMind.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public PlanType Plan { get; set; } = PlanType.Starter;
    public string ApiKeyHash { get; set; } = string.Empty;
    public int MonthlyQueryCount { get; set; }
    public DateTime QueryCountResetAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? StripeCustomerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Schema> Schemas { get; set; } = new List<Schema>();
    public ICollection<QuerySession> Sessions { get; set; } = new List<QuerySession>();
    public ICollection<TenantUser> Users { get; set; } = new List<TenantUser>();

    public int GetQueryLimit() => Plan switch
    {
        PlanType.Starter => 500,
        PlanType.Pro => 5000,
        PlanType.Enterprise => int.MaxValue,
        _ => 500
    };

    public int GetSchemaLimit() => Plan switch
    {
        PlanType.Starter => 3,
        PlanType.Pro => int.MaxValue,
        PlanType.Enterprise => int.MaxValue,
        _ => 3
    };

    public bool HasQueryCapacity() => Plan == PlanType.Enterprise || MonthlyQueryCount < GetQueryLimit();
}
