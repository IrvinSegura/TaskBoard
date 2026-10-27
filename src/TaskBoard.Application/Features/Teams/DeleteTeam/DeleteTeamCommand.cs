using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Teams.DeleteTeam;

public sealed record DeleteTeamCommand(Guid TeamId) : ICommand;
