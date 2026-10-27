namespace TaskBoard.Application.Abstractions.Messaging;

/// <summary>
/// What the outbox recorded about the event being handled: who caused it and when.
/// </summary>
public interface IDomainEventContext
{
    // Null when the change did not come from a signed-in request.
    Guid? ActorId { get; }

    DateTimeOffset OccurredAt { get; }
}
