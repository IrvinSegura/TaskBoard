using FluentValidation;

namespace TaskBoard.Application.Features.Boards.RenameBoard;

internal sealed class RenameBoardCommandValidator : AbstractValidator<RenameBoardCommand>
{
    public RenameBoardCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
