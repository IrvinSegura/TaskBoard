using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Cards.Events;

namespace TaskBoard.Application.Features.Cards.EventHandlers;

internal sealed class CardArchivedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<CardArchivedEvent>
{
    public Task Handle(CardArchivedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CardArchived(new CardArchivedMessage(domainEvent.CardId));
}
