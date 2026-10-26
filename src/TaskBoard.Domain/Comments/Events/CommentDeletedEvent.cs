using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Comments.Events;

public sealed record CommentDeletedEvent(Guid CommentId, Guid CardId, Guid BoardId) : IDomainEvent;
