using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards.Events;

public sealed record CardUpdatedEvent(
    Guid CardId,
    Guid BoardId,
    string Title,
    string? Description,
    CardPriority Priority,
    DateTimeOffset? DueDate)
    : IDomainEvent;
