using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Columns.MoveColumn;

internal sealed class MoveColumnCommandHandler(IBoardRepository boards, IUnitOfWork unitOfWork)
    : ICommandHandler<MoveColumnCommand>
{
    public async Task<Result> Handle(MoveColumnCommand command, CancellationToken cancellationToken)
    {
        var column = await boards.GetColumnAsync(command.ColumnId, cancellationToken);
        if (column is null)
        {
            return BoardErrors.ColumnNotFound(command.ColumnId);
        }

        // Loaded with all its columns so the neighbours are validated against the same board.
        var board = await boards.GetByIdAsync(column.BoardId, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(column.BoardId);
        }

        var result = board.MoveColumn(column.Id, command.PreviousColumnId, command.NextColumnId);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
