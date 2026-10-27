using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Checklists.Events;

namespace TaskBoard.Application.Features.Checklists.EventHandlers;

internal sealed class ChecklistItemUpdatedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<ChecklistItemUpdatedEvent>
{
    public Task Handle(ChecklistItemUpdatedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).ChecklistChanged(new ChecklistChangedMessage(domainEvent.CardId));
}
