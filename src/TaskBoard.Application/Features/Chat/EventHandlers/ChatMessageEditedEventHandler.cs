using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Chat.Events;

namespace TaskBoard.Application.Features.Chat.EventHandlers;

internal sealed class ChatMessageEditedEventHandler(ChatAccess access, IRealtimeNotifier notifier)
    : IDomainEventHandler<ChatMessageEditedEvent>
{
    public async Task Handle(ChatMessageEditedEvent domainEvent, CancellationToken cancellationToken)
    {
        var memberIds = await access.GetMemberIdsAsync(domainEvent.ConversationId, cancellationToken);
        await notifier.Users(memberIds).ChatMessageUpdated(
            new ChatMessageUpdatedMessage(domainEvent.MessageId, domainEvent.ConversationId, domainEvent.Body));
    }
}
