using FluentValidation;

namespace TaskBoard.Application.Features.Teams.ChangeTeamMemberRole;

internal sealed class ChangeTeamMemberRoleCommandValidator : AbstractValidator<ChangeTeamMemberRoleCommand>
{
    public ChangeTeamMemberRoleCommandValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
