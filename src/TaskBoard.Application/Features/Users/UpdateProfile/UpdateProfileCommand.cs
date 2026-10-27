using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Users.UpdateProfile;

public sealed record UpdateProfileCommand(string DisplayName) : ICommand;
