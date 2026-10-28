using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.GetChatContacts;

internal sealed class GetChatContactsQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetChatContactsQuery, IReadOnlyList<ChatUserResponse>>
{
    public async Task<Result<IReadOnlyList<ChatUserResponse>>> Handle(
        GetChatContactsQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var myTeamIds = db.TeamMembers.Where(m => m.UserId == userId).Select(m => m.TeamId);
        var myBoardIds = db.BoardMembers.Where(m => m.UserId == userId).Select(m => m.BoardId);

        var contacts = await db.Users
            .Where(u => u.Id != userId &&
                (db.TeamMembers.Any(m => m.UserId == u.Id && myTeamIds.Contains(m.TeamId)) ||
                    db.BoardMembers.Any(m => m.UserId == u.Id && myBoardIds.Contains(m.BoardId))))
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Email)
            .Select(u => new ChatUserResponse(u.Id, u.DisplayName, u.Email))
            .ToListAsync(cancellationToken);

        return contacts;
    }
}
