using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.Features.Boards.EventHandlers;

internal sealed class BoardRenamedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<BoardRenamedEvent>
{
    public Task Handle(BoardRenamedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).BoardRenamed(
            new BoardRenamedMessage(domainEvent.BoardId, domainEvent.Name));
}
