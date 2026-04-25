using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryMind.Domain.Entities;

namespace QueryMind.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.ApiKeyHash).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Plan).HasConversion<string>();
        builder.HasIndex(t => t.ApiKeyHash).IsUnique();
    }
}
