using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.DeleteCard;

internal sealed class DeleteCardCommandHandler(ICardRepository cards, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteCardCommand>
{
    public async Task<Result> Handle(DeleteCardCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        var result = card.Delete();
        if (result.IsFailure)
        {
            return result;
        }

        cards.Remove(card);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
