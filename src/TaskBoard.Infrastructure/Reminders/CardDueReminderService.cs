using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskBoard.Application.Features.Cards.DueReminders;

namespace TaskBoard.Infrastructure.Reminders;

/// <summary>
/// Sends the due-date reminders every few minutes.
/// </summary>
// Meant for one instance: two at once could both remind of the same card.
internal sealed class CardDueReminderService(
    IServiceScopeFactory scopeFactory,
    IOptions<DueReminderOptions> options,
    TimeProvider timeProvider,
    ILogger<CardDueReminderService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.Interval;
        if (interval <= TimeSpan.Zero)
        {
            return;
        }

        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sent = await scope.ServiceProvider.GetRequiredService<ICardDueReminders>().SendAsync(stoppingToken);
                if (sent > 0)
                {
                    logger.LogInformation("Sent {Count} due date reminders", sent);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Due date reminders failed");
            }
        }
    }
}
