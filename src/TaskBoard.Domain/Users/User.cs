using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Users;

public sealed class User : AggregateRoot
{
    private User()
    {
    }

    public string Email { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public static User Create(string email, string displayName, string passwordHash) => new()
    {
        Email = NormalizeEmail(email),
        DisplayName = displayName.Trim(),
        PasswordHash = passwordHash
    };

    // The caller revokes the user's refresh tokens, so other sessions must sign in with the new password.
    public void ChangePassword(string passwordHash) => PasswordHash = passwordHash;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
