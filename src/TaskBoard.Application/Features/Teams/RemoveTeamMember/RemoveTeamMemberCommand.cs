using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Teams.RemoveTeamMember;

// Also used to leave a team: UserId can be the current user.
public sealed record RemoveTeamMemberCommand(Guid TeamId, Guid UserId) : ICommand;
