using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Activities;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class ActivityEntryConfiguration : IEntityTypeConfiguration<ActivityEntry>
{
    public void Configure(EntityTypeBuilder<ActivityEntry> builder)
    {
        builder.ToTable("ActivityEntries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Subject).HasMaxLength(ActivityEntry.SubjectMaxLength);
        builder.Property(a => a.Detail).HasMaxLength(ActivityEntry.DetailMaxLength);

        // History goes with its board. Cards are not a foreign key: a deleted card keeps its lines.
        builder.HasOne<Board>().WithMany().HasForeignKey(a => a.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.ActorId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.BoardId, a.OccurredAt });
        builder.HasIndex(a => new { a.CardId, a.OccurredAt });
    }
}
