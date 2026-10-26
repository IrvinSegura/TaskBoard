using Microsoft.AspNetCore.SignalR;
using TaskBoard.Application.Abstractions.Realtime;

namespace TaskBoard.Api.Realtime;

internal sealed class SignalRRealtimeNotifier(IHubContext<BoardHub, IBoardClient> hubContext) : IRealtimeNotifier
{
    public IBoardClient Board(Guid boardId) => hubContext.Clients.Group(HubGroups.Board(boardId));

    public IBoardClient User(Guid userId) => hubContext.Clients.User(userId.ToString());
}
