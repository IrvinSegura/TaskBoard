using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Comments;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Comments.EditComment;

internal sealed class EditCommentCommandHandler(
    ICommentRepository comments,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EditCommentCommand>
{
    public async Task<Result> Handle(EditCommentCommand command, CancellationToken cancellationToken)
    {
        var comment = await comments.GetByIdAsync(command.CommentId, cancellationToken);
        if (comment is null)
        {
            return CommentErrors.NotFound(command.CommentId);
        }

        var result = comment.Edit(currentUser.UserId, command.Body);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
