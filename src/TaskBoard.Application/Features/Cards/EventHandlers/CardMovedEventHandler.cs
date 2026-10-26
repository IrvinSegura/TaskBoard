using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Cards.Events;

namespace TaskBoard.Application.Features.Cards.EventHandlers;

internal sealed class CardMovedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<CardMovedEvent>
{
    public Task Handle(CardMovedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CardMoved(
            new CardMovedMessage(
                domainEvent.CardId,
                domainEvent.FromColumnId,
                domainEvent.ToColumnId,
                domainEvent.Position));
}
