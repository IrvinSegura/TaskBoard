using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Boards.ArchiveBoard;

internal sealed class ArchiveBoardCommandHandler(IBoardRepository boards, IUnitOfWork unitOfWork)
    : ICommandHandler<ArchiveBoardCommand>
{
    public async Task<Result> Handle(ArchiveBoardCommand command, CancellationToken cancellationToken)
    {
        var board = await boards.GetByIdAsync(command.BoardId, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(command.BoardId);
        }

        // Idempotent: archiving an archived board succeeds without raising a second event.
        board.Archive();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
