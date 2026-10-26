using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    void Add(RefreshToken refreshToken);
}
