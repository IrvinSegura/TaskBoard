using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Teams.GetTeamMembers;

public sealed record GetTeamMembersQuery(Guid TeamId) : IQuery<IReadOnlyList<TeamMemberResponse>>;
