using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Comments;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Comments.AddComment;

internal sealed class AddCommentCommandHandler(
    ICardRepository cards,
    ICommentRepository comments,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddCommentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddCommentCommand command, CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(command.CardId, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(command.CardId);
        }

        var comment = Comment.Create(card.Id, card.BoardId, currentUser.UserId, command.Body);
        comments.Add(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return comment.Id;
    }
}
