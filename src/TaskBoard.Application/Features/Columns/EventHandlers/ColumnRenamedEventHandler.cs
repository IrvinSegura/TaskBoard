using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.Features.Columns.EventHandlers;

internal sealed class ColumnRenamedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<ColumnRenamedEvent>
{
    public Task Handle(ColumnRenamedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).ColumnRenamed(
            new ColumnRenamedMessage(domainEvent.ColumnId, domainEvent.Name));
}
