using FluentValidation;

namespace TaskBoard.Application.Features.Teams.RenameTeam;

internal sealed class RenameTeamCommandValidator : AbstractValidator<RenameTeamCommand>
{
    public RenameTeamCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
