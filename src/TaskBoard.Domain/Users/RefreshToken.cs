using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Users;

/// <summary>
/// Long-lived credential exchanged for new access tokens. Only its hash is stored, and every use
/// replaces it with a new one (rotation), so a leaked token stops working after its next use.
/// </summary>
public sealed class RefreshToken : Entity
{
    private RefreshToken()
    {
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt) => new()
    {
        UserId = userId,
        TokenHash = tokenHash,
        ExpiresAt = expiresAt
    };

    public bool IsActive(DateTimeOffset now) => !IsRevoked && ExpiresAt > now;

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
