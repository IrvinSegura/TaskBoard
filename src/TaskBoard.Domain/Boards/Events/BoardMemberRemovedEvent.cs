using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards.Events;

public sealed record BoardMemberRemovedEvent(Guid BoardId, Guid UserId) : IDomainEvent;
