using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards.Events;

public sealed record BoardRestoredEvent(Guid BoardId) : IDomainEvent;
