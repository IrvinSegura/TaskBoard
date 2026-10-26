using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards.Events;

public sealed record CardRestoredEvent(Guid CardId, Guid BoardId, Guid ColumnId, string Title, double Position)
    : IDomainEvent;
