using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Auth.ResetPassword;

// Sets a new password with the token from the email, and revokes all of the user's refresh tokens.
public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand;
