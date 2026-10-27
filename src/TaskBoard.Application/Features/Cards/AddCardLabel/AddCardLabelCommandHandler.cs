using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Application.Features.Cards.AddCardLabel;

internal sealed class AddCardLabelCommandHandler(ICardRepository cards, ILabelRepository labels, IUnitOfWork unitOfWork)
    : ICommandHandler<AddCardLabelCommand>
{
    public async Task<Result> Handle(AddCardLabelCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        var label = await labels.GetByIdAsync(command.LabelId, cancellationToken);
        if (label is null)
        {
            return LabelErrors.NotFound(command.LabelId);
        }

        var result = card.AddLabel(label);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
