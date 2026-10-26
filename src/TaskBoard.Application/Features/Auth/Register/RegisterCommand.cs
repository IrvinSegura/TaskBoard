using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Auth.Register;

public sealed record RegisterCommand(string Email, string DisplayName, string Password) : ICommand<Guid>;
