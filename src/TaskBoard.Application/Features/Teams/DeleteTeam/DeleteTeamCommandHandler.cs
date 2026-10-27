using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Teams.DeleteTeam;

internal sealed class DeleteTeamCommandHandler(
    ITeamRepository teams,
    IBoardRepository boards,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteTeamCommand>
{
    public async Task<Result> Handle(DeleteTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null || !team.IsMember(currentUser.UserId))
        {
            return TeamErrors.NotFound(command.TeamId);
        }

        // Read before the delete: afterwards the boards are gone and the event could not say which ones to close.
        var boardIds = await boards.GetTeamBoardIdsAsync(team.Id, cancellationToken);

        var result = team.Delete(currentUser.UserId, boardIds);
        if (result.IsFailure)
        {
            return result;
        }

        teams.Remove(team);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
