using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Cards.Events;

namespace TaskBoard.Application.Features.Cards.EventHandlers;

internal sealed class CardRestoredEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<CardRestoredEvent>
{
    public Task Handle(CardRestoredEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CardRestored(
            new CardRestoredMessage(domainEvent.CardId, domainEvent.ColumnId, domainEvent.Title, domainEvent.Position));
}
