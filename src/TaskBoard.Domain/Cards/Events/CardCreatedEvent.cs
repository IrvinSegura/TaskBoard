using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards.Events;

public sealed record CardCreatedEvent(Guid CardId, Guid BoardId, Guid ColumnId, string Title, double Position)
    : IDomainEvent;
