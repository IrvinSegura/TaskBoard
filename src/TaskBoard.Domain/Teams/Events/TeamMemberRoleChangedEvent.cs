using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Teams.Events;

public sealed record TeamMemberRoleChangedEvent(Guid TeamId, Guid UserId, TeamRole Role, Guid ChangedById) : IDomainEvent;
