using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryMind.Domain.Entities;

namespace QueryMind.Infrastructure.Persistence.Configurations;

public class QuerySessionConfiguration : IEntityTypeConfiguration<QuerySession>
{
    public void Configure(EntityTypeBuilder<QuerySession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.DefaultDialect).HasConversion<string>();
        builder.Property(s => s.Title).HasMaxLength(500);

        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.Sessions)
            .HasForeignKey(s => s.TenantId);

        builder.HasOne(s => s.Schema)
            .WithMany(sc => sc.Sessions)
            .HasForeignKey(s => s.SchemaId)
            .IsRequired(false);
    }
}
