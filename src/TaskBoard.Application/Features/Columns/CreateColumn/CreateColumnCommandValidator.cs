using FluentValidation;

namespace TaskBoard.Application.Features.Columns.CreateColumn;

internal sealed class CreateColumnCommandValidator : AbstractValidator<CreateColumnCommand>
{
    public CreateColumnCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
