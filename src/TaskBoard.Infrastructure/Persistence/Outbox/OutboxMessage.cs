using System.Text.Json;
using TaskBoard.Domain.Common;

namespace TaskBoard.Infrastructure.Persistence.Outbox;

/// <summary>
/// A domain event saved in the same transaction as the change that raised it,
/// then published by <see cref="OutboxProcessor"/> once the commit succeeded.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    // Full name of the event type inside the Domain assembly.
    public string Type { get; init; } = null!;

    public string Content { get; init; } = null!;

    public DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? Error { get; set; }

    public static OutboxMessage Create(IDomainEvent domainEvent, DateTimeOffset occurredAt)
    {
        var eventType = domainEvent.GetType();

        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Type = eventType.FullName!,
            Content = JsonSerializer.Serialize(domainEvent, eventType),
            OccurredAt = occurredAt
        };
    }
}
