namespace TaskBoard.Api.Realtime;

/// <summary>
/// Open hub connections per user, so the server can drop a user from board groups when their access is revoked.
/// </summary>
// TODO: in-memory, so it only sees this instance; with several API instances track connections in the backplane.
public sealed class UserConnectionTracker
{
    private readonly Dictionary<Guid, HashSet<string>> _connections = [];
    private readonly Lock _lock = new();

    public void Add(Guid userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(userId, out var connectionIds))
            {
                connectionIds = [];
                _connections[userId] = connectionIds;
            }

            connectionIds.Add(connectionId);
        }
    }

    public void Remove(Guid userId, string connectionId)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(userId, out var connectionIds) &&
                connectionIds.Remove(connectionId) &&
                connectionIds.Count == 0)
            {
                _connections.Remove(userId);
            }
        }
    }

    public IReadOnlyList<string> GetConnections(Guid userId)
    {
        lock (_lock)
        {
            return _connections.TryGetValue(userId, out var connectionIds) ? [.. connectionIds] : [];
        }
    }
}
