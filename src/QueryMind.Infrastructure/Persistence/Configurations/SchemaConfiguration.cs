using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QueryMind.Infrastructure.Persistence.Configurations;

public class SchemaConfiguration : IEntityTypeConfiguration<Domain.Entities.Schema>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Schema> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(300).IsRequired();
        builder.Property(s => s.Type).HasConversion<string>();
        builder.Property(s => s.ParsedJson).HasColumnType("jsonb");
        builder.HasQueryFilter(s => s.DeletedAt == null);

        builder.HasOne(s => s.Tenant)
            .WithMany(t => t.Schemas)
            .HasForeignKey(s => s.TenantId);
    }
}
