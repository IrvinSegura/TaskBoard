using TaskBoard.Domain.Common;
using TaskBoard.Domain.Notifications;
using TaskBoard.Domain.Users.Events;

namespace TaskBoard.Domain.Users;

public sealed class User : AggregateRoot
{
    private User()
    {
    }

    public string Email { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    // Muted rather than wanted, so a notification type added later reaches people without touching their data.
    public NotificationType[] MutedEmailNotifications { get; private set; } = [];

    // Null until the owner opens the link in the welcome email. Nothing is blocked meanwhile: it only tells
    // that the address really is theirs.
    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    public bool IsEmailVerified => EmailVerifiedAt is not null;

    public static User Create(string email, string displayName, string passwordHash)
    {
        var user = new User
        {
            Email = NormalizeEmail(email),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash
        };

        user.Raise(new UserRegisteredEvent(user.Id));
        return user;
    }

    // Opening the link again keeps the first date.
    public void VerifyEmail(DateTimeOffset now) => EmailVerifiedAt ??= now;

    public void Rename(string displayName) => DisplayName = displayName.Trim();

    // The caller revokes the user's refresh tokens, so other sessions must sign in with the new password.
    public void ChangePassword(string passwordHash) => PasswordHash = passwordHash;

    public bool WantsEmailFor(NotificationType type) => !MutedEmailNotifications.Contains(type);

    public void MuteEmailNotifications(IEnumerable<NotificationType> types) =>
        MutedEmailNotifications = [.. types.Distinct().Order()];

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
