using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Authentication;

/// <summary>
/// Deletes refresh tokens past their expiry, so every login does not leave a row behind forever.
/// </summary>
// Revoked tokens are kept until they expire: reusing one is how a leaked token is detected.
// Safe to run on several instances at once: the delete is a single idempotent statement.
internal sealed class ExpiredRefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider,
    ILogger<ExpiredRefreshTokenCleanupService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.RefreshTokenCleanupInterval;
        if (interval <= TimeSpan.Zero)
        {
            return;
        }

        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Expired refresh token cleanup failed");
            }
        }
    }

    // Returns the number of tokens deleted.
    internal async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = timeProvider.GetUtcNow();

        var deleted = await db.RefreshTokens.Where(t => t.ExpiresAt < now).ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation("Deleted {Count} expired refresh tokens", deleted);
        }

        return deleted;
    }
}
