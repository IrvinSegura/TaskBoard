using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Teams.Events;

public sealed record TeamDeletedEvent(
    Guid TeamId,
    string Name,
    IReadOnlyList<Guid> BoardIds,
    IReadOnlyList<Guid> MemberIds) : IDomainEvent;
