using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Visibility).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<Team>().WithMany().HasForeignKey(b => b.TeamId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Members)
            .WithOne()
            .HasForeignKey(m => m.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Columns)
            .WithOne()
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.TeamId);
    }
}

internal sealed class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => new { m.BoardId, m.UserId }).IsUnique();
    }
}

internal sealed class ColumnConfiguration : IEntityTypeConfiguration<Column>
{
    public void Configure(EntityTypeBuilder<Column> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => new { c.BoardId, c.Position });
    }
}
