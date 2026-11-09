using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Teams.Events;

// RemovedById is the user themself when they left; null in events stored before it existed.
public sealed record TeamMemberRemovedEvent(Guid TeamId, Guid UserId, Guid? RemovedById = null) : IDomainEvent;
