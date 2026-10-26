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

    // TODO: with several API instances, claim rows with FOR UPDATE SKIP LOCKED so a message runs only once.
    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<DomainEventDispatcher>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

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
    }

    private static IDomainEvent Deserialize(OutboxMessage message)
    {
        var eventType = typeof(IDomainEvent).Assembly.GetType(message.Type)
            ?? throw new InvalidOperationException($"Unknown domain event type '{message.Type}'.");

        return (IDomainEvent)(JsonSerializer.Deserialize(message.Content, eventType)
            ?? throw new InvalidOperationException($"Could not deserialize outbox message '{message.Id}'."));
    }
}
