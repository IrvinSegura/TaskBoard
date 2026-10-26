using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards.Events;

public sealed record CardAssignedEvent(Guid CardId, Guid BoardId, string CardTitle, Guid AssigneeId, Guid AssignedById)
    : IDomainEvent;
