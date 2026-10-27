using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Teams.RenameTeam;

public sealed record RenameTeamCommand(Guid TeamId, string Name) : ICommand;
