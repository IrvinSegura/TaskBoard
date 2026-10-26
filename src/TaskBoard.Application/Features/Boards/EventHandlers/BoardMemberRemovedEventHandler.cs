using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.Features.Boards.EventHandlers;

internal sealed class BoardMemberRemovedEventHandler(IRealtimeNotifier notifier)
    : IDomainEventHandler<BoardMemberRemovedEvent>
{
    public Task Handle(BoardMemberRemovedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).BoardMemberRemoved(
            new BoardMemberRemovedMessage(domainEvent.BoardId, domainEvent.UserId));
}
