using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Checklists.Events;

namespace TaskBoard.Application.Features.Checklists.EventHandlers;

internal sealed class ChecklistItemAddedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<ChecklistItemAddedEvent>
{
    public Task Handle(ChecklistItemAddedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).ChecklistChanged(new ChecklistChangedMessage(domainEvent.CardId));
}
