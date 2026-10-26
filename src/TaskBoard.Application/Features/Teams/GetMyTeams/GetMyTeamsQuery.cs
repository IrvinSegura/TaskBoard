using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Teams.GetMyTeams;

public sealed record GetMyTeamsQuery : IQuery<IReadOnlyList<TeamSummaryResponse>>;
