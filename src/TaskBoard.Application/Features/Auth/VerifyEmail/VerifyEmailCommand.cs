using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Auth.VerifyEmail;

public sealed record VerifyEmailCommand(string Token) : ICommand;
