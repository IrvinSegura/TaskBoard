using FluentValidation;

namespace TaskBoard.Application.Features.Columns.RenameColumn;

internal sealed class RenameColumnCommandValidator : AbstractValidator<RenameColumnCommand>
{
    public RenameColumnCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
