using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.AssignCardMember;

internal sealed class AssignCardMemberCommandHandler(
    ICardRepository cards,
    IBoardAccessService boardAccess,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AssignCardMemberCommand>
{
    public async Task<Result> Handle(AssignCardMemberCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        if (await boardAccess.GetRoleAsync(card.BoardId, command.UserId, cancellationToken) is null)
        {
            return CardErrors.AssigneeNotBoardMember;
        }

        var result = card.Assign(command.UserId, currentUser.UserId);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
