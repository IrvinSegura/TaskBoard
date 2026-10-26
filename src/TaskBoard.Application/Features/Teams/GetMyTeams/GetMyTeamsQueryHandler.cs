using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Teams.GetMyTeams;

internal sealed class GetMyTeamsQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetMyTeamsQuery, IReadOnlyList<TeamSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<TeamSummaryResponse>>> Handle(
        GetMyTeamsQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var teams = await (
            from member in db.TeamMembers
            join team in db.Teams on member.TeamId equals team.Id
            where member.UserId == userId
            orderby team.Name
            select new TeamSummaryResponse(team.Id, team.Name, member.Role, team.Members.Count()))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TeamSummaryResponse>>(teams);
    }
}
