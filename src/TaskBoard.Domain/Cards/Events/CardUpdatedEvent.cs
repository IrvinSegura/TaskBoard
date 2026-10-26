using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards.Events;

public sealed record CardUpdatedEvent(Guid CardId, Guid BoardId) : IDomainEvent;
