using TaskBoard.Domain.Comments.Events;
using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Comments;

// Own aggregate (not inside Card) so adding a comment never loads the whole thread.
public sealed class Comment : AggregateRoot
{
    private Comment()
    {
    }

    public Guid CardId { get; private set; }

    // Denormalized so permissions and realtime groups resolve without joining through the card.
    public Guid BoardId { get; private set; }

    public Guid AuthorId { get; private set; }

    public string Body { get; private set; } = null!;

    public bool IsEdited { get; private set; }

    public static Comment Create(Guid cardId, Guid boardId, Guid authorId, string body)
    {
        var comment = new Comment
        {
            CardId = cardId,
            BoardId = boardId,
            AuthorId = authorId,
            Body = body.Trim()
        };

        comment.Raise(new CommentAddedEvent(comment.Id, cardId, boardId, authorId, comment.Body));
        return comment;
    }

    public Result Edit(Guid actorId, string body)
    {
        if (actorId != AuthorId)
        {
            return CommentErrors.NotAuthor;
        }

        Body = body.Trim();
        IsEdited = true;

        Raise(new CommentEditedEvent(Id, CardId, BoardId, Body));
        return Result.Success();
    }

    // Checks the rule and raises the event; the caller removes the comment from its repository.
    public Result Delete(Guid actorId, bool actorIsBoardAdmin)
    {
        if (actorId != AuthorId && !actorIsBoardAdmin)
        {
            return CommentErrors.CannotDelete;
        }

        Raise(new CommentDeletedEvent(Id, CardId, BoardId));
        return Result.Success();
    }
}
