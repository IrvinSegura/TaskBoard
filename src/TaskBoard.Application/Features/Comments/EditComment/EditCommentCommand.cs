using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Comments.EditComment;

public sealed record EditCommentCommand(Guid CommentId, string Body) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Comment;

    Guid IBoardScopedRequest.ScopeId => CommentId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
