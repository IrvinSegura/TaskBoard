using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Attachments;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.FileName).HasMaxLength(AttachmentRules.MaxFileNameLength).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StorageKey).HasMaxLength(200).IsRequired();

        builder.HasOne<Card>().WithMany().HasForeignKey(a => a.CardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UploadedById).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.CardId);

        // Looked up by the orphaned file cleanup.
        builder.HasIndex(a => a.StorageKey).IsUnique();
    }
}
