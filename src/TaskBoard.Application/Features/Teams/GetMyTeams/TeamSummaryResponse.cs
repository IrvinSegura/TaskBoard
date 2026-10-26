using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Teams.GetMyTeams;

public sealed record TeamSummaryResponse(Guid Id, string Name, TeamRole MyRole, int MemberCount);
