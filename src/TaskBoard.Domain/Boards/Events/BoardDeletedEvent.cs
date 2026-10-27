using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards.Events;

public sealed record BoardDeletedEvent(Guid BoardId) : IDomainEvent;
