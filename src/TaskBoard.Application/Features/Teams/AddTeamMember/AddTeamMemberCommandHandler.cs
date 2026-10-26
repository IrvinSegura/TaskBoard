using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Teams.AddTeamMember;

internal sealed class AddTeamMemberCommandHandler(
    ITeamRepository teams,
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddTeamMemberCommand>
{
    public async Task<Result> Handle(AddTeamMemberCommand command, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null || !team.IsMember(currentUser.UserId))
        {
            return TeamErrors.NotFound(command.TeamId);
        }

        // Before the lookup, so plain members cannot use this endpoint to probe which emails are registered.
        if (!team.CanManageMembers(currentUser.UserId))
        {
            return TeamErrors.NotAllowed;
        }

        var user = await users.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFoundByEmail;
        }

        var result = team.AddMember(currentUser.UserId, user.Id, command.Role);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
