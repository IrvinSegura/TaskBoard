using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.Features.Columns.EventHandlers;

internal sealed class ColumnMovedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<ColumnMovedEvent>
{
    public Task Handle(ColumnMovedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).ColumnMoved(
            new ColumnMovedMessage(domainEvent.ColumnId, domainEvent.Position));
}
