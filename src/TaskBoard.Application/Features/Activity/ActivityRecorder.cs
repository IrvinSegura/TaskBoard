using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Activities;
using TaskBoard.Domain.Attachments.Events;
using TaskBoard.Domain.Boards.Events;
using TaskBoard.Domain.Cards.Events;
using TaskBoard.Domain.Comments;
using TaskBoard.Domain.Comments.Events;

namespace TaskBoard.Application.Features.Activity;

/// <summary>
/// Projects board events into activity entries. One class for every event, unlike the other handlers,
/// because it is a single projection: the same lookups and the same way of saving an entry.
/// </summary>
internal sealed class ActivityRecorder(
    IReadDbContext db,
    IActivityRepository activities,
    IUnitOfWork unitOfWork,
    IDomainEventContext context)
    :
    IDomainEventHandler<CardCreatedEvent>,
    IDomainEventHandler<CardUpdatedEvent>,
    IDomainEventHandler<CardMovedEvent>,
    IDomainEventHandler<CardArchivedEvent>,
    IDomainEventHandler<CardRestoredEvent>,
    IDomainEventHandler<CardDeletedEvent>,
    IDomainEventHandler<CardAssignedEvent>,
    IDomainEventHandler<CardUnassignedEvent>,
    IDomainEventHandler<CommentAddedEvent>,
    IDomainEventHandler<AttachmentAddedEvent>,
    IDomainEventHandler<ColumnCreatedEvent>,
    IDomainEventHandler<ColumnRenamedEvent>,
    IDomainEventHandler<ColumnDeletedEvent>,
    IDomainEventHandler<BoardRenamedEvent>,
    IDomainEventHandler<BoardArchivedEvent>,
    IDomainEventHandler<BoardRestoredEvent>,
    IDomainEventHandler<BoardMemberAddedEvent>,
    IDomainEventHandler<BoardMemberRemovedEvent>
{
    public async Task Handle(CardCreatedEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(e.BoardId, e.CardId, ActivityType.CardCreated, e.Title, await ColumnNameAsync(e.ColumnId, cancellationToken), cancellationToken);

    public async Task Handle(CardUpdatedEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(e.BoardId, e.CardId, ActivityType.CardUpdated, e.Title, null, cancellationToken);

    public async Task Handle(CardMovedEvent e, CancellationToken cancellationToken)
    {
        // Moves inside a column (and the renumbering that follows them) are not worth a line.
        if (e.FromColumnId == e.ToColumnId)
        {
            return;
        }

        await RecordAsync(
            e.BoardId,
            e.CardId,
            ActivityType.CardMoved,
            await CardTitleAsync(e.CardId, cancellationToken),
            await ColumnNameAsync(e.ToColumnId, cancellationToken),
            cancellationToken);
    }

    public async Task Handle(CardArchivedEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(e.BoardId, e.CardId, ActivityType.CardArchived, await CardTitleAsync(e.CardId, cancellationToken), null, cancellationToken);

    public async Task Handle(CardRestoredEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(e.BoardId, e.CardId, ActivityType.CardRestored, e.Title, null, cancellationToken);

    public async Task Handle(CardDeletedEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(e.BoardId, e.CardId, ActivityType.CardDeleted, await CardTitleAsync(e.CardId, cancellationToken), null, cancellationToken);

    public async Task Handle(CardAssignedEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(e.BoardId, e.CardId, ActivityType.CardAssigned, e.CardTitle, await UserNameAsync(e.AssigneeId, cancellationToken), cancellationToken);

    public async Task Handle(CardUnassignedEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(
            e.BoardId,
            e.CardId,
            ActivityType.CardUnassigned,
            await CardTitleAsync(e.CardId, cancellationToken),
            await UserNameAsync(e.UserId, cancellationToken),
            cancellationToken);

    public async Task Handle(CommentAddedEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(
            e.BoardId,
            e.CardId,
            ActivityType.CommentAdded,
            await CardTitleAsync(e.CardId, cancellationToken),
            CommentMentions.ToPlainText(e.Body),
            cancellationToken);

    public async Task Handle(AttachmentAddedEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(e.BoardId, e.CardId, ActivityType.AttachmentAdded, await CardTitleAsync(e.CardId, cancellationToken), e.FileName, cancellationToken);

    public Task Handle(ColumnCreatedEvent e, CancellationToken cancellationToken) =>
        RecordAsync(e.BoardId, null, ActivityType.ColumnCreated, e.Name, null, cancellationToken);

    public Task Handle(ColumnRenamedEvent e, CancellationToken cancellationToken) =>
        RecordAsync(e.BoardId, null, ActivityType.ColumnRenamed, e.Name, null, cancellationToken);

    public Task Handle(ColumnDeletedEvent e, CancellationToken cancellationToken) =>
        RecordAsync(e.BoardId, null, ActivityType.ColumnDeleted, null, null, cancellationToken);

    public Task Handle(BoardRenamedEvent e, CancellationToken cancellationToken) =>
        RecordAsync(e.BoardId, null, ActivityType.BoardRenamed, e.Name, null, cancellationToken);

    public Task Handle(BoardArchivedEvent e, CancellationToken cancellationToken) =>
        RecordAsync(e.BoardId, null, ActivityType.BoardArchived, null, null, cancellationToken);

    public Task Handle(BoardRestoredEvent e, CancellationToken cancellationToken) =>
        RecordAsync(e.BoardId, null, ActivityType.BoardRestored, null, null, cancellationToken);

    public async Task Handle(BoardMemberAddedEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(e.BoardId, null, ActivityType.MemberAdded, await UserNameAsync(e.UserId, cancellationToken), null, cancellationToken);

    public async Task Handle(BoardMemberRemovedEvent e, CancellationToken cancellationToken) =>
        await RecordAsync(e.BoardId, null, ActivityType.MemberRemoved, await UserNameAsync(e.UserId, cancellationToken), null, cancellationToken);

    private async Task RecordAsync(
        Guid boardId,
        Guid? cardId,
        ActivityType type,
        string? subject,
        string? detail,
        CancellationToken cancellationToken)
    {
        // The board may be gone by the time the outbox gets here; its history went with it.
        if (!await db.Boards.AnyAsync(b => b.Id == boardId, cancellationToken))
        {
            return;
        }

        activities.Add(ActivityEntry.Create(boardId, cardId, context.ActorId, type, subject, detail, context.OccurredAt));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Falls back to the title the card had in its last entry, for cards deleted since.
    private async Task<string?> CardTitleAsync(Guid cardId, CancellationToken cancellationToken) =>
        await db.Cards.Where(c => c.Id == cardId).Select(c => c.Title).SingleOrDefaultAsync(cancellationToken)
        ?? await db.Activities
            .Where(a => a.CardId == cardId && a.Subject != null)
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => a.Subject)
            .FirstOrDefaultAsync(cancellationToken);

    private Task<string?> ColumnNameAsync(Guid columnId, CancellationToken cancellationToken) =>
        db.Columns.Where(c => c.Id == columnId).Select(c => c.Name).SingleOrDefaultAsync(cancellationToken);

    private Task<string?> UserNameAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.Where(u => u.Id == userId).Select(u => u.DisplayName).SingleOrDefaultAsync(cancellationToken);
}
