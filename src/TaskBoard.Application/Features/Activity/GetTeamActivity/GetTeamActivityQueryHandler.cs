using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Common.Paging;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Activity.GetTeamActivity;

internal sealed class GetTeamActivityQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetTeamActivityQuery, PagedResponse<ActivityResponse>>
{
    public async Task<Result<PagedResponse<ActivityResponse>>> Handle(GetTeamActivityQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (!await db.TeamMembers.AnyAsync(m => m.TeamId == query.TeamId && m.UserId == userId, cancellationToken))
        {
            return TeamErrors.NotFound(query.TeamId);
        }

        var visible = db.VisibleBoardIds(userId);
        var teamBoards = db.Boards.Where(b => b.TeamId == query.TeamId).Select(b => b.Id);

        return await db.Activities
            .Where(a => visible.Contains(a.BoardId) && teamBoards.Contains(a.BoardId))
            .ToFeedAsync(db, query.Page, cancellationToken);
    }
}
