using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Comments.DeleteComment;

public sealed record DeleteCommentCommand(Guid CommentId) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Comment;

    Guid IBoardScopedRequest.ScopeId => CommentId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
