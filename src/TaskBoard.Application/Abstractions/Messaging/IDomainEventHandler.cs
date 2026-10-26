using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Abstractions.Messaging;

/// <summary>
/// Runs after the originating transaction commits (dispatched from the outbox).
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);
}
