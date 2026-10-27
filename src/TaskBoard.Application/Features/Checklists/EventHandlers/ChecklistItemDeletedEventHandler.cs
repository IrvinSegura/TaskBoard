using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Checklists.Events;

namespace TaskBoard.Application.Features.Checklists.EventHandlers;

internal sealed class ChecklistItemDeletedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<ChecklistItemDeletedEvent>
{
    public Task Handle(ChecklistItemDeletedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).ChecklistChanged(new ChecklistChangedMessage(domainEvent.CardId));
}
