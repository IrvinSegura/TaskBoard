using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthTokensResponse>;
