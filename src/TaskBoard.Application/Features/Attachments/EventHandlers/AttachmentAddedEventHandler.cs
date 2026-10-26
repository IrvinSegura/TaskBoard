using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Attachments.Events;

namespace TaskBoard.Application.Features.Attachments.EventHandlers;

internal sealed class AttachmentAddedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<AttachmentAddedEvent>
{
    public Task Handle(AttachmentAddedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).AttachmentAdded(
            new AttachmentAddedMessage(domainEvent.AttachmentId, domainEvent.CardId, domainEvent.FileName));
}
