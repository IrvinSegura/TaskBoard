using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Teams;
using TaskBoard.Infrastructure.Authorization;

namespace TaskBoard.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Evicts cached board roles after a save that changed who can see a board, whichever command or event handler made it.
/// </summary>
internal sealed class BoardRoleCacheInvalidationInterceptor(
    IDistributedCache cache,
    ILogger<BoardRoleCacheInvalidationInterceptor> logger)
    : SaveChangesInterceptor
{
    // The interceptor is a singleton shared by every DbContext, so pending changes are kept per context.
    private readonly ConditionalWeakTable<DbContext, PendingChanges> _pending = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            // Captured before saving: deleted rows are detached afterwards.
            var changes = Capture(context);
            if (changes is not null)
            {
                _pending.AddOrUpdate(context, changes);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // Evicted only after the commit, so a concurrent read cannot cache the old role again in between.
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context && _pending.TryGetValue(context, out var changes))
        {
            _pending.Remove(context);
            await EvictAsync(context, changes, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            _pending.Remove(context);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private static PendingChanges? Capture(DbContext context)
    {
        var boardMembers = context.ChangeTracker.Entries<BoardMember>()
            .Where(e => e.State is EntityState.Added or EntityState.Deleted or EntityState.Modified)
            .Select(e => (e.Entity.BoardId, e.Entity.UserId))
            .ToList();

        // Team membership grants read access to the team's team-visible boards.
        var teamMembers = context.ChangeTracker.Entries<TeamMember>()
            .Where(e => e.State is EntityState.Added or EntityState.Deleted)
            .Select(e => (e.Entity.TeamId, e.Entity.UserId))
            .ToList();

        return boardMembers.Count == 0 && teamMembers.Count == 0 ? null : new PendingChanges(boardMembers, teamMembers);
    }

    private async Task EvictAsync(DbContext context, PendingChanges changes, CancellationToken cancellationToken)
    {
        var keys = changes.BoardMembers.Select(m => BoardRoleCacheKeys.Role(m.BoardId, m.UserId)).ToHashSet();

        if (changes.TeamMembers.Count > 0)
        {
            var teamIds = changes.TeamMembers.Select(m => m.TeamId).Distinct().ToList();
            var boards = await context.Set<Board>()
                .AsNoTracking()
                .Where(b => teamIds.Contains(b.TeamId))
                .Select(b => new { b.Id, b.TeamId })
                .ToListAsync(cancellationToken);

            foreach (var (teamId, userId) in changes.TeamMembers)
            {
                keys.UnionWith(boards.Where(b => b.TeamId == teamId).Select(b => BoardRoleCacheKeys.Role(b.Id, userId)));
            }
        }

        foreach (var key in keys)
        {
            try
            {
                await cache.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The change is already committed; the entry expires on its own shortly.
                logger.LogWarning(ex, "Could not evict board role {Key} from the cache", key);
            }
        }
    }

    private sealed record PendingChanges(
        List<(Guid BoardId, Guid UserId)> BoardMembers,
        List<(Guid TeamId, Guid UserId)> TeamMembers);
}
