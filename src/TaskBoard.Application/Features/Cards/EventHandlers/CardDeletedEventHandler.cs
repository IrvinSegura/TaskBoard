using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Cards.Events;

namespace TaskBoard.Application.Features.Cards.EventHandlers;

internal sealed class CardDeletedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<CardDeletedEvent>
{
    public Task Handle(CardDeletedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CardDeleted(new CardDeletedMessage(domainEvent.CardId));
}
