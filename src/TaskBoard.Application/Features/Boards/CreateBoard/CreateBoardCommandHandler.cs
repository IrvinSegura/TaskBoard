using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Boards.CreateBoard;

internal sealed class CreateBoardCommandHandler(
    ITeamRepository teams,
    IBoardRepository boards,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateBoardCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateBoardCommand command, CancellationToken cancellationToken)
    {
        var team = await teams.GetByIdAsync(command.TeamId, cancellationToken);
        if (team is null || !team.IsMember(currentUser.UserId))
        {
            return TeamErrors.NotFound(command.TeamId);
        }

        var board = Board.Create(team.Id, command.Name, currentUser.UserId, command.Visibility);
        boards.Add(board);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return board.Id;
    }
}
