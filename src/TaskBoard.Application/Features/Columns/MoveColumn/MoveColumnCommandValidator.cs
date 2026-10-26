using FluentValidation;

namespace TaskBoard.Application.Features.Columns.MoveColumn;

internal sealed class MoveColumnCommandValidator : AbstractValidator<MoveColumnCommand>
{
    public MoveColumnCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.PreviousColumnId != x.ColumnId && x.NextColumnId != x.ColumnId)
            .WithMessage("A column cannot be placed next to itself.");

        RuleFor(x => x)
            .Must(x => x.PreviousColumnId is null || x.PreviousColumnId != x.NextColumnId)
            .WithMessage("Previous and next columns must be different.");
    }
}
