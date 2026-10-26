using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Comments;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Comments.DeleteComment;

internal sealed class DeleteCommentCommandHandler(
    ICommentRepository comments,
    IBoardAccessService boardAccess,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteCommentCommand>
{
    public async Task<Result> Handle(DeleteCommentCommand command, CancellationToken cancellationToken)
    {
        var comment = await comments.GetByIdAsync(command.CommentId, cancellationToken);
        if (comment is null)
        {
            return CommentErrors.NotFound(command.CommentId);
        }

        var role = await boardAccess.GetRoleAsync(comment.BoardId, currentUser.UserId, cancellationToken);

        var result = comment.Delete(currentUser.UserId, role == BoardRole.Admin);
        if (result.IsFailure)
        {
            return result;
        }

        comments.Remove(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
