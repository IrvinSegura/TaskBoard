namespace TaskBoard.Application.Features.Auth;

// ExpiresAt is the access token's expiry; the refresh token outlives it.
public sealed record AuthTokensResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
