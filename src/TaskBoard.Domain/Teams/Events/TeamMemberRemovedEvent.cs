using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Teams.Events;

public sealed record TeamMemberRemovedEvent(Guid TeamId, Guid UserId) : IDomainEvent;
