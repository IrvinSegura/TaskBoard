using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Users.Events;

public sealed record UserRegisteredEvent(Guid UserId) : IDomainEvent;
