using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Columns.DeleteColumn;

internal sealed class DeleteColumnCommandHandler(IBoardRepository boards, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteColumnCommand>
{
    public async Task<Result> Handle(DeleteColumnCommand command, CancellationToken cancellationToken)
    {
        var column = await boards.GetColumnAsync(command.ColumnId, cancellationToken);
        if (column is null)
        {
            return BoardErrors.ColumnNotFound(command.ColumnId);
        }

        // Deleted through the board so the archived rule and the domain event stay in the aggregate.
        var board = await boards.GetByIdAsync(column.BoardId, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(column.BoardId);
        }

        var result = board.DeleteColumn(column.Id);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
