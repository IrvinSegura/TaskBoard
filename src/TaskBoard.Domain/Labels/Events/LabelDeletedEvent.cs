using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Labels.Events;

public sealed record LabelDeletedEvent(Guid LabelId, Guid BoardId) : IDomainEvent;
