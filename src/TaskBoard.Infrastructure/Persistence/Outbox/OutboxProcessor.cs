using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskBoard.Domain.Common;

namespace TaskBoard.Infrastructure.Persistence.Outbox;

internal sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollingInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox batch failed");
            }
        }
    }

    // Returns the number of messages handled.
    internal async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<DomainEventDispatcher>();

        var messages = await ClaimBatchAsync(db, cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await dispatcher.DispatchAsync(Deserialize(message), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Marked as processed anyway so one bad message cannot block the queue.
                logger.LogError(ex, "Outbox message {MessageId} of type {Type} failed", message.Id, message.Type);
                message.Error = ex.ToString();
            }

            message.ProcessedAt = timeProvider.GetUtcNow();
        }

        await db.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }

    // Several API instances poll the same table. One statement reserves a batch for this instance:
    // SKIP LOCKED makes concurrent claims take different rows, and the lease keeps the rows reserved
    // while the handlers run outside any transaction. Rows whose lease expired (their instance died) are claimable again.
    private async Task<List<OutboxMessage>> ClaimBatchAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var lockedUntil = now.Add(options.Value.LockDuration);
        var batchSize = options.Value.BatchSize;

        var messages = await db.OutboxMessages
            .FromSql($"""
                UPDATE "OutboxMessages" SET "LockedUntil" = {lockedUntil}
                WHERE "Id" IN (
                    SELECT "Id" FROM "OutboxMessages"
                    WHERE "ProcessedAt" IS NULL AND ("LockedUntil" IS NULL OR "LockedUntil" < {now})
                    ORDER BY "OccurredAt"
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED)
                RETURNING *
                """)
            .ToListAsync(cancellationToken);

        // RETURNING does not keep the subquery's order.
        return [.. messages.OrderBy(m => m.OccurredAt).ThenBy(m => m.Id)];
    }

    private static IDomainEvent Deserialize(OutboxMessage message)
    {
        var eventType = typeof(IDomainEvent).Assembly.GetType(message.Type)
            ?? throw new InvalidOperationException($"Unknown domain event type '{message.Type}'.");

        return (IDomainEvent)(JsonSerializer.Deserialize(message.Content, eventType)
            ?? throw new InvalidOperationException($"Could not deserialize outbox message '{message.Id}'."));
    }
}
