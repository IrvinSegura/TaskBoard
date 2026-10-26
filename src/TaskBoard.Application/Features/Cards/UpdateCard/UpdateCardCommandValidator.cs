using FluentValidation;

namespace TaskBoard.Application.Features.Cards.UpdateCard;

internal sealed class UpdateCardCommandValidator : AbstractValidator<UpdateCardCommand>
{
    public UpdateCardCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(5000);
        RuleFor(x => x.Priority).IsInEnum();
    }
}
