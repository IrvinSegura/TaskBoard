using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Teams.AddTeamMember;

public sealed record AddTeamMemberCommand(Guid TeamId, Guid UserId, TeamRole Role) : ICommand;
