using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Checklists.Events;

public sealed record ChecklistItemAddedEvent(Guid ItemId, Guid CardId, Guid BoardId) : IDomainEvent;
