using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Title).HasMaxLength(ChatRules.MaxTitleLength);
        builder.Property(c => c.DirectKey).HasMaxLength(65);

        // Board and team conversations point at their board or team, and only they do.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Conversations_Target",
            "(\"Kind\" = 'Board') = (\"BoardId\" IS NOT NULL) AND (\"Kind\" = 'Team') = (\"TeamId\" IS NOT NULL)"));

        // The conversation, its messages and their files go away with the board or team.
        builder.HasOne<Board>().WithMany().HasForeignKey(c => c.BoardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Team>().WithMany().HasForeignKey(c => c.TeamId).OnDelete(DeleteBehavior.Cascade);

        // One conversation per board, per team and per pair of people.
        builder.HasIndex(c => c.BoardId).IsUnique().HasFilter("\"BoardId\" IS NOT NULL");
        builder.HasIndex(c => c.TeamId).IsUnique().HasFilter("\"TeamId\" IS NOT NULL");
        builder.HasIndex(c => c.DirectKey).IsUnique().HasFilter("\"DirectKey\" IS NOT NULL");

        builder.HasMany(c => c.Participants)
            .WithOne()
            .HasForeignKey(p => p.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => new { p.ConversationId, p.UserId }).IsUnique();
        builder.HasIndex(p => p.UserId);
    }
}

internal sealed class ConversationReadMarkerConfiguration : IEntityTypeConfiguration<ConversationReadMarker>
{
    public void Configure(EntityTypeBuilder<ConversationReadMarker> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne<Conversation>().WithMany().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(m => new { m.UserId, m.ConversationId }).IsUnique();
    }
}
