using FluentValidation;

namespace TaskBoard.Application.Features.Checklists.UpdateChecklistItem;

internal sealed class UpdateChecklistItemCommandValidator : AbstractValidator<UpdateChecklistItemCommand>
{
    public UpdateChecklistItemCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(500).When(x => x.Text is not null);
        RuleFor(x => x)
            .Must(x => x.Text is not null || x.IsDone is not null)
            .WithName("request")
            .WithMessage("Send the text, the done state or both.");
    }
}
