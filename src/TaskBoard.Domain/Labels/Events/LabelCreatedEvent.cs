using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Labels.Events;

public sealed record LabelCreatedEvent(Guid LabelId, Guid BoardId) : IDomainEvent;
