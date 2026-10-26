using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Boards.RestoreBoard;

internal sealed class RestoreBoardCommandHandler(IBoardRepository boards, IUnitOfWork unitOfWork)
    : ICommandHandler<RestoreBoardCommand>
{
    public async Task<Result> Handle(RestoreBoardCommand command, CancellationToken cancellationToken)
    {
        var board = await boards.GetByIdAsync(command.BoardId, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(command.BoardId);
        }

        // Idempotent: restoring an active board succeeds without raising an event.
        board.Restore();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
