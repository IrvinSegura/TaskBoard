using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Domain.Cards.Events;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Cards.EventHandlers;

internal sealed class CardAssignedEventHandler(IRealtimeNotifier notifier, NotificationPublisher publisher)
    : IDomainEventHandler<CardAssignedEvent>
{
    public async Task Handle(CardAssignedEvent domainEvent, CancellationToken cancellationToken)
    {
        await notifier.Board(domainEvent.BoardId).CardAssigned(
            new CardAssignedMessage(domainEvent.CardId, domainEvent.AssigneeId));

        // No notification when users assign themselves.
        if (domainEvent.AssigneeId == domainEvent.AssignedById)
        {
            return;
        }

        await publisher.PublishAsync(
            domainEvent.AssigneeId,
            NotificationType.CardAssigned,
            $"You were assigned to \"{domainEvent.CardTitle}\".",
            domainEvent.CardId,
            cancellationToken);
    }
}
