using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Realtime;

namespace TaskBoard.Api.Realtime;

/// <summary>
/// Clients call JoinBoard when they open a board and receive that board's events until they leave.
/// Personal notifications reach every connection of the user without joining anything.
/// </summary>
[Authorize]
public sealed class BoardHub(IBoardAccessService boardAccess) : Hub<IBoardClient>
{
    public async Task JoinBoard(Guid boardId)
    {
        if (!Guid.TryParse(Context.UserIdentifier, out var userId))
        {
            throw new HubException("Unauthorized.");
        }

        // Same membership rule as the HTTP endpoints: no access, no events.
        var role = await boardAccess.GetRoleAsync(boardId, userId, Context.ConnectionAborted);
        if (role is null)
        {
            throw new HubException("Board not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, HubGroups.Board(boardId), Context.ConnectionAborted);
    }

    public Task LeaveBoard(Guid boardId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, HubGroups.Board(boardId), Context.ConnectionAborted);

    // TODO: when a member is removed from a board, also remove their connections from its group.
}
