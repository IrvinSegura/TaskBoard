using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Abstractions.Authorization;

public static class AuthorizationErrors
{
    // Also returned to non-members, so they cannot probe which boards exist.
    public static readonly Error ResourceNotFound =
        Error.NotFound("Authorization.ResourceNotFound", "The requested resource was not found.");

    public static readonly Error InsufficientRole =
        Error.Forbidden("Authorization.InsufficientRole", "Your board role does not allow this action.");
}
