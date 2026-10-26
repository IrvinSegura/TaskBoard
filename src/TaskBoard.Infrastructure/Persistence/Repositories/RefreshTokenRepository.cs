using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(ApplicationDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

    public void Add(RefreshToken refreshToken) => db.RefreshTokens.Add(refreshToken);
}
