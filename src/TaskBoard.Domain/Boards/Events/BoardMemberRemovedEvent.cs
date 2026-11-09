using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards.Events;

// RemovedById is null when the user left along with a team, which tells them on its own.
public sealed record BoardMemberRemovedEvent(Guid BoardId, Guid UserId, Guid? RemovedById = null) : IDomainEvent;
