using FluentValidation;

namespace TaskBoard.Application.Features.Teams.AddTeamMember;

internal sealed class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Role).IsInEnum();
    }
}
