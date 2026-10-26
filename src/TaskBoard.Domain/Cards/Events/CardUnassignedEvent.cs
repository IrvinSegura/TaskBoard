using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards.Events;

public sealed record CardUnassignedEvent(Guid CardId, Guid BoardId, Guid UserId) : IDomainEvent;
