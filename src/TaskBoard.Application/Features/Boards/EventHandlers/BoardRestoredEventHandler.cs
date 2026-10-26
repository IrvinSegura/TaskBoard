using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.Features.Boards.EventHandlers;

internal sealed class BoardRestoredEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<BoardRestoredEvent>
{
    public Task Handle(BoardRestoredEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).BoardRestored(new BoardRestoredMessage(domainEvent.BoardId));
}
