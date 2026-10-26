using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Infrastructure.Authorization;

/// <summary>
/// Caches board roles, which every board-scoped request asks for. The cache is shared by all API instances
/// when Redis is configured. <see cref="BoardRoleCacheInvalidationInterceptor"/> evicts entries when memberships change.
/// </summary>
internal sealed class CachedBoardAccessService(
    BoardAccessService inner,
    IDistributedCache cache,
    ILogger<CachedBoardAccessService> logger)
    : IBoardAccessService
{
    // Short on purpose: eviction keeps entries fresh, and the expiry bounds a stale value
    // written by a read that raced with a membership change.
    private static readonly DistributedCacheEntryOptions EntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    // Users without access are cached too, so probing a board does not hit the database every time.
    private const string NoAccess = "None";

    public async Task<BoardRole?> GetRoleAsync(Guid boardId, Guid userId, CancellationToken cancellationToken)
    {
        var key = BoardRoleCacheKeys.Role(boardId, userId);

        // A cache outage must not take authorization down with it: fall back to the database.
        try
        {
            var cached = await cache.GetStringAsync(key, cancellationToken);
            if (cached is not null)
            {
                return cached == NoAccess ? null : Enum.Parse<BoardRole>(cached);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not read board role {Key} from the cache", key);
        }

        var role = await inner.GetRoleAsync(boardId, userId, cancellationToken);

        try
        {
            await cache.SetStringAsync(key, role?.ToString() ?? NoAccess, EntryOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not write board role {Key} to the cache", key);
        }

        return role;
    }

    public Task<bool> IsArchivedAsync(Guid boardId, CancellationToken cancellationToken) =>
        inner.IsArchivedAsync(boardId, cancellationToken);

    public Task<Guid?> ResolveBoardIdAsync(BoardScope scope, Guid scopeId, CancellationToken cancellationToken) =>
        inner.ResolveBoardIdAsync(scope, scopeId, cancellationToken);
}
