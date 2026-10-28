using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Activities;
using TaskBoard.Domain.Attachments;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Checklists;
using TaskBoard.Domain.Comments;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Favorites;
using TaskBoard.Domain.Labels;
using TaskBoard.Domain.Notifications;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Users;
using TaskBoard.Infrastructure.Persistence.Outbox;

namespace TaskBoard.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IUnitOfWork, IReadDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    public DbSet<Board> Boards => Set<Board>();

    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();

    public DbSet<Column> Columns => Set<Column>();

    public DbSet<Card> Cards => Set<Card>();

    public DbSet<CardAssignment> CardAssignments => Set<CardAssignment>();

    public DbSet<CardLabel> CardLabels => Set<CardLabel>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<Label> Labels => Set<Label>();

    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    public DbSet<ActivityEntry> Activities => Set<ActivityEntry>();

    public DbSet<Favorite> Favorites => Set<Favorite>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();

    public DbSet<ConversationReadMarker> ConversationReadMarkers => Set<ConversationReadMarker>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<ChatAttachment> ChatAttachments => Set<ChatAttachment>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    IQueryable<User> IReadDbContext.Users => Users.AsNoTracking();

    IQueryable<Team> IReadDbContext.Teams => Teams.AsNoTracking();

    IQueryable<TeamMember> IReadDbContext.TeamMembers => TeamMembers.AsNoTracking();

    IQueryable<Board> IReadDbContext.Boards => Boards.AsNoTracking();

    IQueryable<BoardMember> IReadDbContext.BoardMembers => BoardMembers.AsNoTracking();

    IQueryable<Column> IReadDbContext.Columns => Columns.AsNoTracking();

    IQueryable<Card> IReadDbContext.Cards => Cards.AsNoTracking();

    IQueryable<Comment> IReadDbContext.Comments => Comments.AsNoTracking();

    IQueryable<Attachment> IReadDbContext.Attachments => Attachments.AsNoTracking();

    IQueryable<Notification> IReadDbContext.Notifications => Notifications.AsNoTracking();

    IQueryable<Label> IReadDbContext.Labels => Labels.AsNoTracking();

    IQueryable<ChecklistItem> IReadDbContext.ChecklistItems => ChecklistItems.AsNoTracking();

    IQueryable<ActivityEntry> IReadDbContext.Activities => Activities.AsNoTracking();

    IQueryable<Favorite> IReadDbContext.Favorites => Favorites.AsNoTracking();

    IQueryable<Conversation> IReadDbContext.Conversations => Conversations.AsNoTracking();

    IQueryable<ConversationParticipant> IReadDbContext.ConversationParticipants => ConversationParticipants.AsNoTracking();

    IQueryable<ConversationReadMarker> IReadDbContext.ConversationReadMarkers => ConversationReadMarkers.AsNoTracking();

    IQueryable<ChatMessage> IReadDbContext.ChatMessages => ChatMessages.AsNoTracking();

    IQueryable<ChatAttachment> IReadDbContext.ChatAttachments => ChatAttachments.AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;

            if (typeof(Entity).IsAssignableFrom(clrType))
            {
                // Ids are created in code. Without this, a child added to a loaded aggregate
                // (e.g. a new Column) would be treated as existing and EF would try an UPDATE.
                modelBuilder.Entity(clrType).Property(nameof(Entity.Id)).ValueGeneratedNever();
            }

            if (typeof(AggregateRoot).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Ignore(nameof(AggregateRoot.DomainEvents));
            }
        }
    }
}
