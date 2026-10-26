using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Users;

public static class UserErrors
{
    public static readonly Error EmailTaken =
        Error.Conflict("Users.EmailTaken", "The email is already registered.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Users.InvalidCredentials", "Invalid email or password.");

    public static Error NotFound(Guid userId) =>
        Error.NotFound("Users.NotFound", $"User '{userId}' was not found.");
}
