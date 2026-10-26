using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Auth;

/// <summary>
/// Issues and revokes a user's tokens. The caller saves the unit of work.
/// </summary>
internal sealed class AuthTokenService(
    ITokenProvider tokenProvider,
    IRefreshTokenRepository refreshTokens,
    TimeProvider timeProvider)
{
    public AuthTokensResponse Issue(User user)
    {
        var accessToken = tokenProvider.CreateAccessToken(user);
        var refreshToken = tokenProvider.CreateRefreshToken();

        refreshTokens.Add(RefreshToken.Create(user.Id, refreshToken.Hash, refreshToken.ExpiresAt));

        return new AuthTokensResponse(accessToken.Token, accessToken.ExpiresAt, refreshToken.Token, refreshToken.ExpiresAt);
    }

    // Ends every session of the user. Access tokens already issued stay valid until they expire.
    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        foreach (var refreshToken in await refreshTokens.GetActiveForUserAsync(userId, now, cancellationToken))
        {
            refreshToken.Revoke(now);
        }
    }
}
