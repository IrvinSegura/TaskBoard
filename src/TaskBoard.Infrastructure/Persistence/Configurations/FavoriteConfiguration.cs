using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Favorites;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.HasKey(f => f.Id);
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Favorites_OneTarget",
            "(\"BoardId\" IS NULL) <> (\"TeamId\" IS NULL)"));

        // A star goes away with its user, board or team.
        builder.HasOne<User>().WithMany().HasForeignKey(f => f.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Board>().WithMany().HasForeignKey(f => f.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Team>().WithMany().HasForeignKey(f => f.TeamId).OnDelete(DeleteBehavior.Cascade);

        // One star per user and target; the unique indexes also serve the "my favorites" lookups.
        builder.HasIndex(f => new { f.UserId, f.BoardId }).IsUnique().HasFilter("\"BoardId\" IS NOT NULL");
        builder.HasIndex(f => new { f.UserId, f.TeamId }).IsUnique().HasFilter("\"TeamId\" IS NOT NULL");
        builder.HasIndex(f => f.BoardId);
        builder.HasIndex(f => f.TeamId);
    }
}
