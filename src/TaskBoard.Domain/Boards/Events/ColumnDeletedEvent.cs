using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards.Events;

public sealed record ColumnDeletedEvent(Guid BoardId, Guid ColumnId) : IDomainEvent;
