using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Auth.ChangePassword;

// Changes the current user's password and revokes all of their refresh tokens.
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand;
