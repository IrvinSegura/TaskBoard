using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Activity;
using TaskBoard.Application.Features.Teams.GetMyTeams;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Favorites.GetMyFavorites;

internal sealed class GetMyFavoritesQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetMyFavoritesQuery, FavoritesResponse>
{
    public async Task<Result<FavoritesResponse>> Handle(GetMyFavoritesQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var mine = db.Favorites.Where(f => f.UserId == userId);

        var teams = await (
            from favorite in mine
            join member in db.TeamMembers on favorite.TeamId equals (Guid?)member.TeamId
            join team in db.Teams on member.TeamId equals team.Id
            where member.UserId == userId
            orderby team.Name
            select new TeamSummaryResponse(team.Id, team.Name, member.Role, team.Members.Count()))
            .ToListAsync(cancellationToken);

        var visible = db.VisibleBoardIds(userId);
        var boards = await (
            from favorite in mine
            join board in db.Boards on favorite.BoardId equals (Guid?)board.Id
            join team in db.Teams on board.TeamId equals team.Id
            where !board.IsArchived && visible.Contains(board.Id)
            orderby board.Name
            select new FavoriteBoardResponse(board.Id, board.Name, board.Visibility, team.Id, team.Name))
            .ToListAsync(cancellationToken);

        return new FavoritesResponse(teams, boards);
    }
}
