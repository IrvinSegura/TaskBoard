using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.ArchiveCard;

internal sealed class ArchiveCardCommandHandler(ICardRepository cards, IUnitOfWork unitOfWork)
    : ICommandHandler<ArchiveCardCommand>
{
    public async Task<Result> Handle(ArchiveCardCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        // Idempotent: archiving an archived card succeeds without raising a second event.
        card.Archive();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
