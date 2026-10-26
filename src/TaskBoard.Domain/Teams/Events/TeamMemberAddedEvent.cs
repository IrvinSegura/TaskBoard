using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Teams.Events;

public sealed record TeamMemberAddedEvent(Guid TeamId, Guid UserId) : IDomainEvent;
