using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Labels;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(5000);
        builder.Property(c => c.Priority).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<Board>().WithMany().HasForeignKey(c => c.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Column>().WithMany().HasForeignKey(c => c.ColumnId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.CreatedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Assignments)
            .WithOne()
            .HasForeignKey(a => a.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Labels)
            .WithOne()
            .HasForeignKey(l => l.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.ColumnId, c.Position });
        builder.HasIndex(c => c.BoardId);
    }
}

internal sealed class CardAssignmentConfiguration : IEntityTypeConfiguration<CardAssignment>
{
    public void Configure(EntityTypeBuilder<CardAssignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(a => new { a.CardId, a.UserId }).IsUnique();
    }
}

internal sealed class CardLabelConfiguration : IEntityTypeConfiguration<CardLabel>
{
    public void Configure(EntityTypeBuilder<CardLabel> builder)
    {
        builder.HasKey(l => l.Id);
        builder.HasOne<Label>().WithMany().HasForeignKey(l => l.LabelId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(l => new { l.CardId, l.LabelId }).IsUnique();
    }
}
