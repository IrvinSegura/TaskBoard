using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards.Events;

public sealed record ColumnCreatedEvent(Guid BoardId, Guid ColumnId, string Name, double Position) : IDomainEvent;
