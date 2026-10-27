using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Checklists;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Text).HasMaxLength(500).IsRequired();

        builder.HasOne<Card>().WithMany().HasForeignKey(i => i.CardId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.CardId, i.Position });
        builder.HasIndex(i => i.BoardId);
    }
}
