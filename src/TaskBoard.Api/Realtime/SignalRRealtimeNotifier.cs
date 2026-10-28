using Microsoft.AspNetCore.SignalR;
using TaskBoard.Application.Abstractions.Realtime;

namespace TaskBoard.Api.Realtime;

internal sealed class SignalRRealtimeNotifier(
    IHubContext<BoardHub, IBoardClient> hubContext,
    IBoardGroupEvictor evictor)
    : IRealtimeNotifier
{
    public IBoardClient Board(Guid boardId) => hubContext.Clients.Group(HubGroups.Board(boardId));

    public IBoardClient User(Guid userId) => hubContext.Clients.User(userId.ToString());

    public IBoardClient Users(IReadOnlyCollection<Guid> userIds) =>
        hubContext.Clients.Users(userIds.Select(id => id.ToString()).ToList());

    public Task RemoveFromBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken) =>
        evictor.EvictAsync(boardId, userId, cancellationToken);
}
