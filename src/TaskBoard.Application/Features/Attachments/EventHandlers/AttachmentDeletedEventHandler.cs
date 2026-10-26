using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Application.Abstractions.Storage;
using TaskBoard.Domain.Attachments.Events;

namespace TaskBoard.Application.Features.Attachments.EventHandlers;

internal sealed class AttachmentDeletedEventHandler(IFileStorage fileStorage, IRealtimeNotifier notifier)
    : IDomainEventHandler<AttachmentDeletedEvent>
{
    public async Task Handle(AttachmentDeletedEvent domainEvent, CancellationToken cancellationToken)
    {
        await notifier.Board(domainEvent.BoardId).AttachmentDeleted(
            new AttachmentDeletedMessage(domainEvent.AttachmentId, domainEvent.CardId));

        // Runs after the delete committed, so a failed transaction never loses the file.
        await fileStorage.DeleteAsync(domainEvent.StorageKey, cancellationToken);
    }
}
