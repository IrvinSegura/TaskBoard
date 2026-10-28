using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Favorites;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Favorites.SetTeamFavorite;

internal sealed class SetTeamFavoriteCommandHandler(
    IReadDbContext db,
    IFavoriteRepository favorites,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetTeamFavoriteCommand>
{
    public async Task<Result> Handle(SetTeamFavoriteCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Non-members get the same answer as for a team that does not exist.
        if (!await db.TeamMembers.AnyAsync(m => m.TeamId == command.TeamId && m.UserId == userId, cancellationToken))
        {
            return TeamErrors.NotFound(command.TeamId);
        }

        var favorite = await favorites.GetTeamFavoriteAsync(userId, command.TeamId, cancellationToken);

        if (command.IsFavorite && favorite is null)
        {
            favorites.Add(Favorite.ForTeam(userId, command.TeamId));
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
