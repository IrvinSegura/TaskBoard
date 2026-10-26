using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Users;

public static class UserErrors
{
    public static readonly Error EmailTaken =
        Error.Conflict("Users.EmailTaken", "The email is already registered.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Users.InvalidCredentials", "Invalid email or password.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Users.InvalidRefreshToken", "The refresh token is invalid, expired or revoked.");

    // Validation rather than Unauthorized: the session is fine, only the typed password is wrong.
    public static readonly Error WrongPassword =
        Error.Validation("Users.WrongPassword", "The current password is incorrect.");

    public static readonly Error NotFoundByEmail =
        Error.NotFound("Users.NotFoundByEmail", "No user is registered with that email.");

    public static Error NotFound(Guid userId) =>
        Error.NotFound("Users.NotFound", $"User '{userId}' was not found.");
}
