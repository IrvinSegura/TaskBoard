using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Boards.GetTeamBoards;

internal sealed class GetTeamBoardsQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetTeamBoardsQuery, IReadOnlyList<BoardSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<BoardSummaryResponse>>> Handle(
        GetTeamBoardsQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var isTeamMember = await db.TeamMembers
            .AnyAsync(m => m.TeamId == query.TeamId && m.UserId == userId, cancellationToken);
        if (!isTeamMember)
        {
            return TeamErrors.NotFound(query.TeamId);
        }

        // Same visibility rule as BoardAccessService: team-visible boards, or private ones the user belongs to.
        var boards = await db.Boards
            .Where(b => b.TeamId == query.TeamId && !b.IsArchived)
            .Where(b => b.Visibility == BoardVisibility.Team || b.Members.Any(m => m.UserId == userId))
            .OrderBy(b => b.Name)
            .Select(b => new BoardSummaryResponse(b.Id, b.Name, b.Visibility))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<BoardSummaryResponse>>(boards);
    }
}
