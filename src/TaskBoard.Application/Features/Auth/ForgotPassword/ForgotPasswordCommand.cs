using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Auth.ForgotPassword;

// Emails a link to choose a new password, if the address has an account.
public sealed record ForgotPasswordCommand(string Email) : ICommand;
