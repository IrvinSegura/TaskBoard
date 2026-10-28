using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Application.Abstractions.Storage;
using TaskBoard.Domain.Chat.Events;

namespace TaskBoard.Application.Features.Chat.EventHandlers;

internal sealed class ChatMessageDeletedEventHandler(
    ChatAccess access,
    IFileStorage fileStorage,
    IRealtimeNotifier notifier)
    : IDomainEventHandler<ChatMessageDeletedEvent>
{
    public async Task Handle(ChatMessageDeletedEvent domainEvent, CancellationToken cancellationToken)
    {
        var memberIds = await access.GetMemberIdsAsync(domainEvent.ConversationId, cancellationToken);
        await notifier.Users(memberIds).ChatMessageDeleted(
            new ChatMessageDeletedMessage(domainEvent.MessageId, domainEvent.ConversationId));

        // Runs after the delete committed, so a failed transaction never loses the files.
        foreach (var storageKey in domainEvent.StorageKeys)
        {
            await fileStorage.DeleteAsync(storageKey, cancellationToken);
        }
    }
}
