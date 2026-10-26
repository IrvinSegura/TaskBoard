using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.CreateCard;

internal sealed class CreateCardCommandHandler(
    IBoardRepository boards,
    ICardRepository cards,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateCardCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCardCommand command, CancellationToken cancellationToken)
    {
        var column = await boards.GetColumnAsync(command.ColumnId, cancellationToken);
        if (column is null)
        {
            return BoardErrors.ColumnNotFound(command.ColumnId);
        }

        var lastPosition = await cards.GetLastPositionAsync(column.Id, cancellationToken);
        var card = Card.Create(
            column.BoardId,
            column.Id,
            command.Title,
            command.Description,
            currentUser.UserId,
            PositionCalculator.After(lastPosition));

        cards.Add(card);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return card.Id;
    }
}
