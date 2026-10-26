using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Comments;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Body).HasMaxLength(4000).IsRequired();

        builder.HasOne<Card>().WithMany().HasForeignKey(c => c.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.CardId, c.CreatedAt });
    }
}
