using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Abstractions.Authentication;

public interface ITokenProvider
{
    AccessToken CreateAccessToken(User user);

    // A random opaque token and the hash to store; the plain value is only ever sent to the client.
    GeneratedRefreshToken CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public sealed record GeneratedRefreshToken(string Token, string Hash, DateTimeOffset ExpiresAt);
