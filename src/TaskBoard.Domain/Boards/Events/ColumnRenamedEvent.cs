using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards.Events;

public sealed record ColumnRenamedEvent(Guid BoardId, Guid ColumnId, string Name) : IDomainEvent;
