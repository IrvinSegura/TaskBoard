using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.Features.Boards.EventHandlers;

internal sealed class BoardDeletedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<BoardDeletedEvent>
{
    public Task Handle(BoardDeletedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).BoardDeleted(new BoardDeletedMessage(domainEvent.BoardId));
}
