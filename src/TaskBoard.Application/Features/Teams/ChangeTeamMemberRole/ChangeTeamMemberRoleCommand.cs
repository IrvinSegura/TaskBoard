using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Teams;

namespace TaskBoard.Application.Features.Teams.ChangeTeamMemberRole;

public sealed record ChangeTeamMemberRoleCommand(Guid TeamId, Guid UserId, TeamRole Role) : ICommand;
