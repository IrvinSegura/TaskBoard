using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Teams.AddTeamMember;

// By email, because that is what the inviter knows; user ids are only visible to people who already share a team.
public sealed record AddTeamMemberCommand(Guid TeamId, string Email, TeamRole Role) : ICommand;
