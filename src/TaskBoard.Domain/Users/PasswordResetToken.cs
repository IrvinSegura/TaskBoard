using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Users;

/// <summary>
/// Proves the holder can read the account's email, which lets them choose a new password once.
/// Only its hash is stored, so reading the table does not give access to any account.
/// </summary>
public sealed class PasswordResetToken : Entity
{
    // Asking again before this passes sends nothing, so nobody can flood someone's inbox with reset emails.
    public static readonly TimeSpan RequestCooldown = TimeSpan.FromMinutes(1);

    private PasswordResetToken()
    {
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ConsumedAt { get; private set; }

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt) => new()
    {
        UserId = userId,
        TokenHash = tokenHash,
        ExpiresAt = expiresAt
    };

    public bool IsActive(DateTimeOffset now) => ConsumedAt is null && ExpiresAt > now;

    // Spent by using it, or by something that makes it pointless: a newer token or a password change.
    public void Consume(DateTimeOffset now) => ConsumedAt ??= now;
}
