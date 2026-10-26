using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Boards.AddBoardMember;

internal sealed class AddBoardMemberCommandHandler(
    IBoardRepository boards,
    ITeamRepository teams,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddBoardMemberCommand>
{
    public async Task<Result> Handle(AddBoardMemberCommand command, CancellationToken cancellationToken)
    {
        var board = await boards.GetByIdAsync(command.BoardId, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(command.BoardId);
        }

        var team = await teams.GetByIdAsync(board.TeamId, cancellationToken);
        if (team is null || !team.IsMember(command.UserId))
        {
            return BoardErrors.UserNotInTeam;
        }

        var result = board.AddMember(command.UserId, command.Role);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
