using TaskBoard.Domain.Favorites;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IFavoriteRepository
{
    Task<Favorite?> GetBoardFavoriteAsync(Guid userId, Guid boardId, CancellationToken cancellationToken);

    Task<Favorite?> GetTeamFavoriteAsync(Guid userId, Guid teamId, CancellationToken cancellationToken);

    void Add(Favorite favorite);

    void Remove(Favorite favorite);
}
