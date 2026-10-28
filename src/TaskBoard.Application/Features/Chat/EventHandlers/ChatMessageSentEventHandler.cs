using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Chat.Events;

namespace TaskBoard.Application.Features.Chat.EventHandlers;

// The sender gets it too: their other tabs show the message, and the sending tab ignores the duplicate.
internal sealed class ChatMessageSentEventHandler(IReadDbContext db, ChatAccess access, IRealtimeNotifier notifier)
    : IDomainEventHandler<ChatMessageSentEvent>
{
    public async Task Handle(ChatMessageSentEvent domainEvent, CancellationToken cancellationToken)
    {
        // Gone (deleted, or its board or team was) before the outbox got to it.
        var message = await ChatQueries.GetMessageAsync(db, domainEvent.MessageId, cancellationToken);
        if (message is null)
        {
            return;
        }

        var memberIds = await access.GetMemberIdsAsync(domainEvent.ConversationId, cancellationToken);
        await notifier.Users(memberIds).ChatMessageReceived(new ChatMessageReceivedMessage(message));
    }
}
