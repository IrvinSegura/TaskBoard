using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Boards.DeleteBoard;

internal sealed class DeleteBoardCommandHandler(IBoardRepository boards, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteBoardCommand>
{
    public async Task<Result> Handle(DeleteBoardCommand command, CancellationToken cancellationToken)
    {
        var board = await boards.GetByIdAsync(command.BoardId, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(command.BoardId);
        }

        var result = board.Delete();
        if (result.IsFailure)
        {
            return result;
        }

        boards.Remove(board);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
