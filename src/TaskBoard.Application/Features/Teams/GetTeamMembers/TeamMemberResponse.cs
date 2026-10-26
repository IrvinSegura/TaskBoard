using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Teams.GetTeamMembers;

public sealed record TeamMemberResponse(Guid UserId, string DisplayName, string Email, TeamRole Role);
