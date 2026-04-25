using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QueryMind.Domain.Entities;

namespace QueryMind.Infrastructure.Persistence.Configurations;

public class QueryMessageConfiguration : IEntityTypeConfiguration<QueryMessage>
{
    public void Configure(EntityTypeBuilder<QueryMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Role).HasConversion<string>();
        builder.Property(m => m.Dialect).HasConversion<string>();
        builder.Property(m => m.Content).IsRequired();
        builder.Property(m => m.ModelVersion).HasMaxLength(100);

        builder.HasOne(m => m.Session)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.SessionId);

        builder.HasOne(m => m.Feedback)
            .WithOne(f => f.Message)
            .HasForeignKey<QueryFeedback>(f => f.MessageId);
    }
}
