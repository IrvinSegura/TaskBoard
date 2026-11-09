using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Domain.Boards.Events;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Boards.EventHandlers;

internal sealed class BoardMemberRemovedEventHandler(
    IRealtimeNotifier notifier,
    IBoardAccessService boardAccess,
    IReadDbContext db,
    NotificationPublisher publisher)
    : IDomainEventHandler<BoardMemberRemovedEvent>
{
    public async Task Handle(BoardMemberRemovedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Team members keep read access to team-visible boards, so only drop users who lost all access.
        // Dropping them first also sends them the notice directly, so on a single instance the group
        // message below no longer reaches them twice.
        var lostAccess = await boardAccess.GetRoleAsync(domainEvent.BoardId, domainEvent.UserId, cancellationToken) is null;
        if (lostAccess)
        {
            await notifier.RemoveFromBoardAsync(domainEvent.BoardId, domainEvent.UserId, cancellationToken);
        }

        // The rest of the board updates its member list.
        await notifier.Board(domainEvent.BoardId).BoardMemberRemoved(
            new BoardMemberRemovedMessage(domainEvent.BoardId, domainEvent.UserId));

        // Told only when someone else removed them and they can no longer see it: leaving needs no notice,
        // and a team member keeps reading a team-visible board.
        if (lostAccess && domainEvent.RemovedById is { } removedById && removedById != domainEvent.UserId)
        {
            await NotifyAsync(domainEvent, removedById, cancellationToken);
        }
    }

    private async Task NotifyAsync(BoardMemberRemovedEvent domainEvent, Guid removedById, CancellationToken cancellationToken)
    {
        var boardName = await db.Boards
            .Where(b => b.Id == domainEvent.BoardId)
            .Select(b => b.Name)
            .SingleOrDefaultAsync(cancellationToken);
        if (boardName is null)
        {
            return;
        }

        var actorName = await db.Users
            .Where(u => u.Id == removedById)
            .Select(u => u.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);

        // No resource: the board no longer opens for them.
        await publisher.PublishAsync(
            domainEvent.UserId,
            NotificationType.RemovedFromBoard,
            $"You were removed from the board \"{boardName}\".",
            null,
            NotificationEmailFacts.RemovedFromBoard(boardName, actorName),
            cancellationToken);
    }
}
