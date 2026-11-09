using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class PasswordResetTokenRepository(ApplicationDbContext db) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.PasswordResetTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<PasswordResetToken>> GetActiveForUserAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.ConsumedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

    public void Add(PasswordResetToken token) => db.PasswordResetTokens.Add(token);
}
