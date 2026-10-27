using FluentValidation;

namespace TaskBoard.Application.Features.Labels.CreateLabel;

internal sealed class CreateLabelCommandValidator : AbstractValidator<CreateLabelCommand>
{
    public CreateLabelCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).IsInEnum();
    }
}
