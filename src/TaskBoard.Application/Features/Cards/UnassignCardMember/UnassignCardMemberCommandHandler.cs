using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.UnassignCardMember;

internal sealed class UnassignCardMemberCommandHandler(ICardRepository cards, IUnitOfWork unitOfWork)
    : ICommandHandler<UnassignCardMemberCommand>
{
    public async Task<Result> Handle(UnassignCardMemberCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        var result = card.Unassign(command.UserId);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
