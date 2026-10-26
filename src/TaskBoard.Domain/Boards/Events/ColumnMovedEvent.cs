using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards.Events;

public sealed record ColumnMovedEvent(Guid BoardId, Guid ColumnId, double Position) : IDomainEvent;
