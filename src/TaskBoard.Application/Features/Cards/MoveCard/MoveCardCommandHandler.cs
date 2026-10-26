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

        var result = card.MoveTo(column.Id, PositionCalculator.Between(previous, next));
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CardMovedResponse(card.Id, card.ColumnId, card.Position);
    }
}
