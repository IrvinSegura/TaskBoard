using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Cards.Events;

namespace TaskBoard.Application.Features.Cards.EventHandlers;

internal sealed class CardUpdatedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<CardUpdatedEvent>
{
    public Task Handle(CardUpdatedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CardUpdated(
            new CardUpdatedMessage(
                domainEvent.CardId,
                domainEvent.Title,
                domainEvent.Description,
                domainEvent.Priority,
                domainEvent.DueDate));
}
