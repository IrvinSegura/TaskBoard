using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards.Events;

public sealed record CardMovedEvent(Guid CardId, Guid BoardId, Guid FromColumnId, Guid ToColumnId, double Position)
    : IDomainEvent;
