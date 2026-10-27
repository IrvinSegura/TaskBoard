using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Checklists.Events;

public sealed record ChecklistItemUpdatedEvent(Guid ItemId, Guid CardId, Guid BoardId) : IDomainEvent;
