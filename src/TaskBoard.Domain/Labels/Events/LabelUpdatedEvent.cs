using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Labels.Events;

public sealed record LabelUpdatedEvent(Guid LabelId, Guid BoardId) : IDomainEvent;
