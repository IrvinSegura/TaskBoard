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

        if (!await users.ExistsAsync(command.UserId, cancellationToken))
        {
            return UserErrors.NotFound(command.UserId);
        }

        var result = team.AddMember(currentUser.UserId, command.UserId, command.Role);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
