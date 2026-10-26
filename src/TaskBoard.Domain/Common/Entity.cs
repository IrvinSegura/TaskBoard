namespace TaskBoard.Domain.Common;

public abstract class Entity
{
    // Keys are generated in code (UUID v7, time-ordered) so aggregates know their id before saving.
    public Guid Id { get; protected init; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }
}
