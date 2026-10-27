using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Checklists;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Checklists.UpdateChecklistItem;

internal sealed class UpdateChecklistItemCommandHandler(
    ICardRepository cards,
    IChecklistItemRepository checklist,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateChecklistItemCommand>
{
    public async Task<Result> Handle(UpdateChecklistItemCommand command, CancellationToken cancellationToken)
    {
        var item = await checklist.GetByIdAsync(command.ItemId, cancellationToken);

        // Authorization ran against the card in the route, so an item of another card does not exist here.
        if (item is null || item.CardId != command.CardId)
        {
            return ChecklistErrors.ItemNotFound(command.ItemId);
        }

        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        if (card.IsArchived)
        {
            return CardErrors.Archived;
        }

        item.Update(command.Text, command.IsDone);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
