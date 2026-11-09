using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Boards.RemoveBoardMember;

internal sealed class RemoveBoardMemberCommandHandler(
    IBoardRepository boards,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveBoardMemberCommand>
{
    public async Task<Result> Handle(RemoveBoardMemberCommand command, CancellationToken cancellationToken)
    {
        var board = await boards.GetByIdAsync(command.BoardId, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(command.BoardId);
        }

        var result = board.RemoveMember(command.UserId, currentUser.UserId);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
