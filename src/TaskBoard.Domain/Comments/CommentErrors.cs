using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Comments;

public static class CommentErrors
{
    public static readonly Error NotAuthor =
        Error.Forbidden("Comments.NotAuthor", "Only the author can modify this comment.");

    public static readonly Error CannotDelete =
        Error.Forbidden("Comments.CannotDelete", "Only the author or a board admin can delete this comment.");

    public static Error NotFound(Guid commentId) =>
        Error.NotFound("Comments.NotFound", $"Comment '{commentId}' was not found.");
}
