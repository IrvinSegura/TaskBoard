using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Infrastructure.Persistence.Outbox;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Type).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Content).HasColumnType("jsonb").IsRequired();

        // Only pending messages are polled, so index just those.
        builder.HasIndex(m => m.OccurredAt).HasFilter("\"ProcessedAt\" IS NULL");
    }
}
