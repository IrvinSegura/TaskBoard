using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Columns.CreateColumn;

internal sealed class CreateColumnCommandHandler(IBoardRepository boards, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateColumnCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateColumnCommand command, CancellationToken cancellationToken)
    {
        var board = await boards.GetByIdAsync(command.BoardId, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(command.BoardId);
        }

        var result = board.AddColumn(command.Name);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Value.Id;
    }
}
