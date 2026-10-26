using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.MoveCard;

internal sealed class MoveCardCommandHandler(
    ICardRepository cards,
    IBoardRepository boards,
    IUnitOfWork unitOfWork)
    : ICommandHandler<MoveCardCommand, CardMovedResponse>
{
    public async Task<Result<CardMovedResponse>> Handle(MoveCardCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        // Authorization only checked the card's board; the target column must be on that same board.
        var column = await boards.GetColumnAsync(command.TargetColumnId, cancellationToken);
        if (column is null || column.BoardId != card.BoardId)
        {
            return CardErrors.InvalidTargetColumn;
        }

        var previous = command.PreviousCardId is { } previousId
            ? await cards.GetPositionInColumnAsync(previousId, column.Id, cancellationToken)
            : null;
        var next = command.NextCardId is { } nextId
            ? await cards.GetPositionInColumnAsync(nextId, column.Id, cancellationToken)
            : null;

        if (PositionCalculator.NeedsRebalance(previous, next))
        {
            (previous, next) = await RenumberColumnAsync(card, command, cancellationToken);
        }

        var result = card.MoveTo(column.Id, PositionCalculator.Between(previous, next));
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CardMovedResponse(card.Id, card.ColumnId, card.Position);
    }

    // The neighbours are too close to split again: spread the target column's other cards evenly,
    // keeping their order, and return the neighbours' new positions. Saved together with the move.
    private async Task<(double? Previous, double? Next)> RenumberColumnAsync(
        Card movingCard,
        MoveCardCommand command,
        CancellationToken cancellationToken)
    {
        var columnCards = (await cards.GetColumnCardsAsync(command.TargetColumnId, cancellationToken))
            .Where(c => c.Id != movingCard.Id)
            .ToList();

        for (var i = 0; i < columnCards.Count; i++)
        {
            columnCards[i].Reposition(PositionCalculator.Sequential(i));
        }

        return (
            columnCards.SingleOrDefault(c => c.Id == command.PreviousCardId)?.Position,
            columnCards.SingleOrDefault(c => c.Id == command.NextCardId)?.Position);
    }
}
