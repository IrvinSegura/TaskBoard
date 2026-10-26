using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Boards.Events;

public sealed record BoardMemberAddedEvent(Guid BoardId, Guid UserId, BoardRole Role) : IDomainEvent;
