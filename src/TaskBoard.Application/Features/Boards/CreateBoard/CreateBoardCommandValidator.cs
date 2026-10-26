using FluentValidation;

namespace TaskBoard.Application.Features.Boards.CreateBoard;

internal sealed class CreateBoardCommandValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Visibility).IsInEnum();
    }
}
