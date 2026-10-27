using FluentValidation;

namespace TaskBoard.Application.Features.Checklists.AddChecklistItem;

internal sealed class AddChecklistItemCommandValidator : AbstractValidator<AddChecklistItemCommand>
{
    public AddChecklistItemCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(500);
    }
}
