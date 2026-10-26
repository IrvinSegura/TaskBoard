using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Comments.Events;

public sealed record CommentAddedEvent(Guid CommentId, Guid CardId, Guid BoardId, Guid AuthorId, string Body)
    : IDomainEvent;
