using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Chat.Events;

public sealed record ChatMessageEditedEvent(Guid MessageId, Guid ConversationId, string? Body) : IDomainEvent;
