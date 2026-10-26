using FluentValidation;

namespace TaskBoard.Application.Features.Cards.MoveCard;

internal sealed class MoveCardCommandValidator : AbstractValidator<MoveCardCommand>
{
    public MoveCardCommandValidator()
    {
        RuleFor(x => x.TargetColumnId).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.PreviousCardId != x.CardId && x.NextCardId != x.CardId)
            .WithMessage("A card cannot be placed next to itself.");

        RuleFor(x => x)
            .Must(x => x.PreviousCardId is null || x.PreviousCardId != x.NextCardId)
            .WithMessage("Previous and next cards must be different.");
    }
}
