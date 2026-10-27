using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Color).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<Board>().WithMany().HasForeignKey(l => l.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(l => l.BoardId);
    }
}
