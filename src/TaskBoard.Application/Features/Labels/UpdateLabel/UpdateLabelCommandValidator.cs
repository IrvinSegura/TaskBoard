using FluentValidation;

namespace TaskBoard.Application.Features.Labels.UpdateLabel;

internal sealed class UpdateLabelCommandValidator : AbstractValidator<UpdateLabelCommand>
{
    public UpdateLabelCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).IsInEnum();
    }
}
