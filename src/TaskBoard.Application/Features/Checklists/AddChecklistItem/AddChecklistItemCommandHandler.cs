using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Checklists;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Checklists.AddChecklistItem;

internal sealed class AddChecklistItemCommandHandler(
    ICardRepository cards,
    IChecklistItemRepository checklist,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddChecklistItemCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddChecklistItemCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        if (card.IsArchived)
        {
            return CardErrors.Archived;
        }

        var lastPosition = await checklist.GetLastPositionAsync(card.Id, cancellationToken);
        var item = ChecklistItem.Create(card.Id, card.BoardId, command.Text, PositionCalculator.After(lastPosition));
        checklist.Add(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
