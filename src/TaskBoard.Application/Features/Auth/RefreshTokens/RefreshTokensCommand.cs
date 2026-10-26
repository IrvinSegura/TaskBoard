using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Auth.RefreshTokens;

// Exchanges a refresh token for a new access token and a new refresh token; the old one stops working.
public sealed record RefreshTokensCommand(string RefreshToken) : ICommand<AuthTokensResponse>;
