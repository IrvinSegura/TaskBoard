using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Cards.Events;

namespace TaskBoard.Application.Features.Cards.EventHandlers;

internal sealed class CardUnassignedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<CardUnassignedEvent>
{
    public Task Handle(CardUnassignedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CardUnassigned(
            new CardUnassignedMessage(domainEvent.CardId, domainEvent.UserId));
}
