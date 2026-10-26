using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using TaskBoard.Application.Abstractions.Realtime;

namespace TaskBoard.Api.Realtime;

/// <summary>
/// Drops a user's open connections from a board's hub group, on every API instance that holds one.
/// </summary>
internal interface IBoardGroupEvictor
{
    Task EvictAsync(Guid boardId, Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// Evicts the connections open on this instance. Enough on its own when the API runs as a single instance.
/// </summary>
internal sealed class LocalBoardGroupEvictor(
    IHubContext<BoardHub, IBoardClient> hubContext,
    UserConnectionTracker connections)
    : IBoardGroupEvictor
{
    public async Task EvictAsync(Guid boardId, Guid userId, CancellationToken cancellationToken)
    {
        var notice = new BoardMemberRemovedMessage(boardId, userId);

        foreach (var connectionId in connections.GetConnections(userId))
        {
            // Sent to the connection itself, right before it leaves the group, so the client always learns
            // it lost access. A group message could arrive after the eviction, or never when access came
            // only from the team.
            await hubContext.Clients.Client(connectionId).BoardMemberRemoved(notice);
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, HubGroups.Board(boardId), cancellationToken);
        }
    }
}

/// <summary>
/// Publishes the eviction on Redis; every instance, this one included, evicts its own connections
/// in <see cref="RedisBoardGroupEvictionListener"/>. Each instance tracks only the connections it holds,
/// so nothing goes stale when an instance dies.
/// </summary>
internal sealed class RedisBoardGroupEvictor(IConnectionMultiplexer redis) : IBoardGroupEvictor
{
    public static readonly RedisChannel Channel = RedisChannel.Literal("taskboard:board-group-evictions");

    public Task EvictAsync(Guid boardId, Guid userId, CancellationToken cancellationToken) =>
        redis.GetSubscriber().PublishAsync(Channel, JsonSerializer.Serialize(new BoardGroupEviction(boardId, userId)));
}

internal sealed class RedisBoardGroupEvictionListener(
    IConnectionMultiplexer redis,
    LocalBoardGroupEvictor localEvictor,
    ILogger<RedisBoardGroupEvictionListener> logger)
    : IHostedService
{
    private ChannelMessageQueue? _queue;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _queue = await redis.GetSubscriber().SubscribeAsync(RedisBoardGroupEvictor.Channel);
        _queue.OnMessage(HandleAsync);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_queue is not null)
        {
            await _queue.UnsubscribeAsync();
        }
    }

    private async Task HandleAsync(ChannelMessage message)
    {
        try
        {
            var eviction = JsonSerializer.Deserialize<BoardGroupEviction>(message.Message.ToString())!;
            await localEvictor.EvictAsync(eviction.BoardId, eviction.UserId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not apply board group eviction {Message}", message.Message.ToString());
        }
    }
}

internal sealed record BoardGroupEviction(Guid BoardId, Guid UserId);
