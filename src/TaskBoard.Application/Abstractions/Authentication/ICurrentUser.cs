namespace TaskBoard.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    // Throws when there is no authenticated user; only use it in authenticated requests.
    Guid UserId { get; }
}
