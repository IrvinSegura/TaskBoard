using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Auth.Logout;

// Revokes the session's refresh token. The access token stays valid until it expires.
public sealed record LogoutCommand(string RefreshToken) : ICommand;
