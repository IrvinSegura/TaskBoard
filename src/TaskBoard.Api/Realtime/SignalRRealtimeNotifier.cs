using Microsoft.AspNetCore.SignalR;
using TaskBoard.Application.Abstractions.Realtime;

namespace TaskBoard.Api.Realtime;

internal sealed class SignalRRealtimeNotifier(
    IHubContext<BoardHub, IBoardClient> hubContext,
    UserConnectionTracker connections)
    : IRealtimeNotifier
{
    public IBoardClient Board(Guid boardId) => hubContext.Clients.Group(HubGroups.Board(boardId));

    public IBoardClient User(Guid userId) => hubContext.Clients.User(userId.ToString());

    public async Task RemoveFromBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken)
    {
        foreach (var connectionId in connections.GetConnections(userId))
        {
            await hubContext.Groups.RemoveFromGroupAsync(connectionId, HubGroups.Board(boardId), cancellationToken);
        }
    }
}
