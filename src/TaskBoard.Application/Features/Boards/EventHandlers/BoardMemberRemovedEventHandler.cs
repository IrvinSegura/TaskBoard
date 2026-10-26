using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.Features.Boards.EventHandlers;

internal sealed class BoardMemberRemovedEventHandler(IRealtimeNotifier notifier, IBoardAccessService boardAccess)
    : IDomainEventHandler<BoardMemberRemovedEvent>
{
    public async Task Handle(BoardMemberRemovedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Sent before dropping the user so their own client also learns it was removed.
        await notifier.Board(domainEvent.BoardId).BoardMemberRemoved(
            new BoardMemberRemovedMessage(domainEvent.BoardId, domainEvent.UserId));

        // Team members keep read access to team-visible boards, so only drop users who lost all access.
        if (await boardAccess.GetRoleAsync(domainEvent.BoardId, domainEvent.UserId, cancellationToken) is null)
        {
            await notifier.RemoveFromBoardAsync(domainEvent.BoardId, domainEvent.UserId, cancellationToken);
        }
    }
}
