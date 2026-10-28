using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskBoard.Application.Abstractions.Storage;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Storage;

/// <summary>
/// Deletes stored files that no card or chat attachment row points to, e.g. when the upload succeeded
/// but saving the row failed or the process died in between.
/// </summary>
// Safe to run on several instances at once: deleting a file twice is a no-op.
internal sealed class OrphanedFileCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<FileStorageOptions> options,
    TimeProvider timeProvider,
    ILogger<OrphanedFileCleanupService> logger)
    : BackgroundService
{
    private const int BatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.OrphanCleanupInterval;
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
                logger.LogError(ex, "Orphaned file cleanup failed");
            }
        }
    }

    // Returns the number of files deleted.
    internal async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var cutoff = timeProvider.GetUtcNow() - options.Value.OrphanGracePeriod;
        var deleted = 0;
        var batch = new List<string>(BatchSize);

        await foreach (var file in fileStorage.ListAsync(cancellationToken))
        {
            if (file.LastModified >= cutoff)
            {
                continue;
            }

            batch.Add(file.StorageKey);
            if (batch.Count == BatchSize)
            {
                deleted += await DeleteOrphansAsync(db, fileStorage, batch, cancellationToken);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            deleted += await DeleteOrphansAsync(db, fileStorage, batch, cancellationToken);
        }

        if (deleted > 0)
        {
            logger.LogInformation("Deleted {Count} orphaned files", deleted);
        }

        return deleted;
    }

    private static async Task<int> DeleteOrphansAsync(
        ApplicationDbContext db,
        IFileStorage fileStorage,
        List<string> storageKeys,
        CancellationToken cancellationToken)
    {
        var referenced = await db.Attachments
            .Where(a => storageKeys.Contains(a.StorageKey))
            .Select(a => a.StorageKey)
            .Concat(db.ChatAttachments.Where(a => storageKeys.Contains(a.StorageKey)).Select(a => a.StorageKey))
            .ToListAsync(cancellationToken);

        var orphans = storageKeys.Except(referenced).ToList();
        foreach (var storageKey in orphans)
        {
            await fileStorage.DeleteAsync(storageKey, cancellationToken);
        }

        return orphans.Count;
    }
}
