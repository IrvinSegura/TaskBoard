using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards.Events;

public sealed record CardLabelsChangedEvent(Guid CardId, Guid BoardId) : IDomainEvent;
