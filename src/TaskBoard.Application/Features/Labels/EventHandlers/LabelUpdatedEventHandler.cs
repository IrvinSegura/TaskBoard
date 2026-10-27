using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Labels.Events;

namespace TaskBoard.Application.Features.Labels.EventHandlers;

internal sealed class LabelUpdatedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<LabelUpdatedEvent>
{
    public Task Handle(LabelUpdatedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).LabelsChanged(new LabelsChangedMessage(domainEvent.BoardId));
}
