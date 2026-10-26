using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Comments.Events;

public sealed record CommentEditedEvent(Guid CommentId, Guid CardId, Guid BoardId, string Body) : IDomainEvent;
