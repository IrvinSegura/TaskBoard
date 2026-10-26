using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Teams.GetTeamMembers;

internal sealed class GetTeamMembersQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetTeamMembersQuery, IReadOnlyList<TeamMemberResponse>>
{
    public async Task<Result<IReadOnlyList<TeamMemberResponse>>> Handle(
        GetTeamMembersQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var isTeamMember = await db.TeamMembers
            .AnyAsync(m => m.TeamId == query.TeamId && m.UserId == userId, cancellationToken);
        if (!isTeamMember)
        {
            return TeamErrors.NotFound(query.TeamId);
        }

        // Owner first, then admins, then members; alphabetical within each role.
        var members = await (
            from member in db.TeamMembers
            join user in db.Users on member.UserId equals user.Id
            where member.TeamId == query.TeamId
            orderby member.Role descending, user.DisplayName
            select new TeamMemberResponse(user.Id, user.DisplayName, user.Email, member.Role))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TeamMemberResponse>>(members);
    }
}
