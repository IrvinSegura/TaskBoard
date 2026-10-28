using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Favorites;

namespace TaskBoard.Application.Features.Favorites.SetBoardFavorite;

internal sealed class SetBoardFavoriteCommandHandler(
    IFavoriteRepository favorites,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetBoardFavoriteCommand>
{
    public async Task<Result> Handle(SetBoardFavoriteCommand command, CancellationToken cancellationToken)
    {
        // The authorization decorator already checked that the user can see the board.
        var userId = currentUser.UserId;
        var favorite = await favorites.GetBoardFavoriteAsync(userId, command.BoardId, cancellationToken);

        if (command.IsFavorite && favorite is null)
        {
            favorites.Add(Favorite.ForBoard(userId, command.BoardId));
        }
        else if (!command.IsFavorite && favorite is not null)
        {
            favorites.Remove(favorite);
        }
        else
        {
            return Result.Success();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
