using FluentValidation;

namespace TaskBoard.Application.Features.Cards.CreateCard;

internal sealed class CreateCardCommandValidator : AbstractValidator<CreateCardCommand>
{
    public CreateCardCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(5000);
    }
}
