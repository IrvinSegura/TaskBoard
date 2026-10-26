using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Teams.CreateTeam;

public sealed record CreateTeamCommand(string Name) : ICommand<Guid>;
