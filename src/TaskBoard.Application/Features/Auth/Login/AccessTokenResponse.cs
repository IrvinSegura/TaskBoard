namespace TaskBoard.Application.Features.Auth.Login;

public sealed record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
