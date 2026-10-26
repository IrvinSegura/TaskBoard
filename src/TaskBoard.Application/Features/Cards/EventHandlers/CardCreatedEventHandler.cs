using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Cards.Events;

namespace TaskBoard.Application.Features.Cards.EventHandlers;

internal sealed class CardCreatedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<CardCreatedEvent>
{
    public Task Handle(CardCreatedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CardCreated(
            new CardCreatedMessage(domainEvent.CardId, domainEvent.ColumnId, domainEvent.Title, domainEvent.Position));
}
