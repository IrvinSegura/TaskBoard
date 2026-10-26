using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Columns.RenameColumn;

internal sealed class RenameColumnCommandHandler(IBoardRepository boards, IUnitOfWork unitOfWork)
    : ICommandHandler<RenameColumnCommand>
{
    public async Task<Result> Handle(RenameColumnCommand command, CancellationToken cancellationToken)
    {
        var column = await boards.GetColumnAsync(command.ColumnId, cancellationToken);
        if (column is null)
        {
            return BoardErrors.ColumnNotFound(command.ColumnId);
        }

        // Renamed through the board so the archived rule and the domain event stay in the aggregate.
        var board = await boards.GetByIdAsync(column.BoardId, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(column.BoardId);
        }

        var result = board.RenameColumn(column.Id, command.Name);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
