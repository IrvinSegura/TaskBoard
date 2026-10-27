using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Labels.Events;

namespace TaskBoard.Application.Features.Labels.EventHandlers;

internal sealed class LabelDeletedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<LabelDeletedEvent>
{
    public Task Handle(LabelDeletedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).LabelsChanged(new LabelsChangedMessage(domainEvent.BoardId));
}
