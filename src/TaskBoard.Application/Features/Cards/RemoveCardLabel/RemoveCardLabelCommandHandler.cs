using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.RemoveCardLabel;

internal sealed class RemoveCardLabelCommandHandler(ICardRepository cards, IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveCardLabelCommand>
{
    public async Task<Result> Handle(RemoveCardLabelCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        var result = card.RemoveLabel(command.LabelId);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
