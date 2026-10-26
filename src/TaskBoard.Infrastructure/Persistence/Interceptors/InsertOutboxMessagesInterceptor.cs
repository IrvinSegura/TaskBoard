using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskBoard.Domain.Common;
using TaskBoard.Infrastructure.Persistence.Outbox;

namespace TaskBoard.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Turns pending domain events into outbox rows inside the same SaveChanges,
/// so an event exists only if its change was committed.
/// </summary>
internal sealed class InsertOutboxMessagesInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessages(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void InsertOutboxMessages(DbContext context)
    {
        var occurredAt = timeProvider.GetUtcNow();

        var messages = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .SelectMany(aggregate =>
            {
                var domainEvents = aggregate.DomainEvents.ToList();
                aggregate.ClearDomainEvents();
                return domainEvents;
            })
            .Select(domainEvent => OutboxMessage.Create(domainEvent, occurredAt))
            .ToList();

        context.Set<OutboxMessage>().AddRange(messages);
    }
}
