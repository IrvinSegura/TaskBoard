using FluentValidation;

namespace TaskBoard.Application.Features.Teams.CreateTeam;

internal sealed class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
