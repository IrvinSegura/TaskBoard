using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        // SHA-256 as hex.
        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(t => t.UserId);

        // Used by the expired token cleanup.
        builder.HasIndex(t => t.ExpiresAt);

        // Mapped to PostgreSQL's xmin, so two requests rotating the same token cannot both succeed.
        builder.Property<uint>("Version").IsRowVersion();
    }
}
