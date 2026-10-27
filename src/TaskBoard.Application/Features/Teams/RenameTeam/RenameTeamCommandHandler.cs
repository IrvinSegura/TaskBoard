using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Teams.RenameTeam;

internal sealed class RenameTeamCommandHandler(
    ITeamRepository teams,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RenameTeamCommand>
{
    public async Task<Result> Handle(RenameTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null || !team.IsMember(currentUser.UserId))
        {
            return TeamErrors.NotFound(command.TeamId);
        }

        var result = team.Rename(currentUser.UserId, command.Name);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
