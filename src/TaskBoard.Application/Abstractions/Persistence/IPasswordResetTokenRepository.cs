using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<PasswordResetToken>> GetActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    void Add(PasswordResetToken token);
}
