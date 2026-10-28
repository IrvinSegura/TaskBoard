using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Favorites;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class FavoriteRepository(ApplicationDbContext db) : IFavoriteRepository
{
    public Task<Favorite?> GetBoardFavoriteAsync(Guid userId, Guid boardId, CancellationToken cancellationToken) =>
        db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.BoardId == boardId, cancellationToken);

    public Task<Favorite?> GetTeamFavoriteAsync(Guid userId, Guid teamId, CancellationToken cancellationToken) =>
        db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.TeamId == teamId, cancellationToken);

    public void Add(Favorite favorite) => db.Favorites.Add(favorite);

    public void Remove(Favorite favorite) => db.Favorites.Remove(favorite);
}
