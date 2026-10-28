using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Chat.Events;

public sealed record ChatMessageSentEvent(Guid MessageId, Guid ConversationId, Guid SenderId) : IDomainEvent;
