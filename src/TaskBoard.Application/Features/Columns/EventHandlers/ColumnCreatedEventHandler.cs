using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.Features.Columns.EventHandlers;

internal sealed class ColumnCreatedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<ColumnCreatedEvent>
{
    public Task Handle(ColumnCreatedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).ColumnCreated(
            new ColumnCreatedMessage(domainEvent.ColumnId, domainEvent.Name, domainEvent.Position));
}
