using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Chat.Events;

// Carries the storage keys because the attachment rows are gone by the time the event is handled.
public sealed record ChatMessageDeletedEvent(Guid MessageId, Guid ConversationId, IReadOnlyList<string> StorageKeys)
    : IDomainEvent;
