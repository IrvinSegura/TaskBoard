using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards.Events;

public sealed record BoardRenamedEvent(Guid BoardId, string Name) : IDomainEvent;
