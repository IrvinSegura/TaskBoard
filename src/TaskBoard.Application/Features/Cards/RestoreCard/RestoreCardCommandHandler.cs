using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.RestoreCard;

internal sealed class RestoreCardCommandHandler(ICardRepository cards, IUnitOfWork unitOfWork)
    : ICommandHandler<RestoreCardCommand>
{
    public async Task<Result> Handle(RestoreCardCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        // Idempotent: restoring an active card succeeds without raising an event.
        card.Restore();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
