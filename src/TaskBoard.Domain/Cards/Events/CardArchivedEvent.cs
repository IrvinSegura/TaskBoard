using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Cards.Events;

public sealed record CardArchivedEvent(Guid CardId, Guid BoardId) : IDomainEvent;
