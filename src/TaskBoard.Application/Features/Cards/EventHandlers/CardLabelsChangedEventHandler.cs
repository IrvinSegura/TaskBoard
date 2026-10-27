using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Cards.Events;

namespace TaskBoard.Application.Features.Cards.EventHandlers;

internal sealed class CardLabelsChangedEventHandler(IRealtimeNotifier notifier)
    : IDomainEventHandler<CardLabelsChangedEvent>
{
    public Task Handle(CardLabelsChangedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CardLabelsChanged(new CardLabelsChangedMessage(domainEvent.CardId));
}
