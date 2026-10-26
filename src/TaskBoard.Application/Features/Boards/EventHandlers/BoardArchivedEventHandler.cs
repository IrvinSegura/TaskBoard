using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.Features.Boards.EventHandlers;

internal sealed class BoardArchivedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<BoardArchivedEvent>
{
    public Task Handle(BoardArchivedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).BoardArchived(new BoardArchivedMessage(domainEvent.BoardId));
}
