using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.Features.Columns.EventHandlers;

internal sealed class ColumnDeletedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<ColumnDeletedEvent>
{
    public Task Handle(ColumnDeletedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).ColumnDeleted(new ColumnDeletedMessage(domainEvent.ColumnId));
}
