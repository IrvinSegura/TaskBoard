using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.UpdateCard;

internal sealed class UpdateCardCommandHandler(ICardRepository cards, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCardCommand>
{
    public async Task<Result> Handle(UpdateCardCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        var result = card.Update(command.Title, command.Description, command.Priority, command.DueDate);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
