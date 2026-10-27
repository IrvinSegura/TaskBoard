using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Labels.Events;

namespace TaskBoard.Application.Features.Labels.EventHandlers;

internal sealed class LabelCreatedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<LabelCreatedEvent>
{
    public Task Handle(LabelCreatedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).LabelsChanged(new LabelsChangedMessage(domainEvent.BoardId));
}
