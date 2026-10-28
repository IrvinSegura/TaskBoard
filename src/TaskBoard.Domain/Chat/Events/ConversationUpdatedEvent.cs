using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Chat.Events;

// A group was renamed or changed participants. UserIds are everyone affected, people who just left included.
public sealed record ConversationUpdatedEvent(Guid ConversationId, IReadOnlyList<Guid> UserIds) : IDomainEvent;
