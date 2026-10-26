using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Boards.RenameBoard;

internal sealed class RenameBoardCommandHandler(IBoardRepository boards, IUnitOfWork unitOfWork)
    : ICommandHandler<RenameBoardCommand>
{
    public async Task<Result> Handle(RenameBoardCommand command, CancellationToken cancellationToken)
    {
        var board = await boards.GetByIdAsync(command.BoardId, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(command.BoardId);
        }

        var result = board.Rename(command.Name);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
