using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Infrastructure.Persistence.Outbox;

// Scoped: the outbox processor sets it before dispatching each message to the handlers of the same scope.
internal sealed class DomainEventContext : IDomainEventContext
{
    public Guid? ActorId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public void Set(Guid? actorId, DateTimeOffset occurredAt)
    {
        ActorId = actorId;
        OccurredAt = occurredAt;
    }
}
