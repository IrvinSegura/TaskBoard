using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Abstractions.Realtime;

public sealed record ColumnCreatedMessage(Guid ColumnId, string Name, double Position);

public sealed record CardCreatedMessage(Guid CardId, Guid ColumnId, string Title, double Position);

public sealed record CardMovedMessage(Guid CardId, Guid FromColumnId, Guid ToColumnId, double Position);

public sealed record CardAssignedMessage(Guid CardId, Guid UserId);

public sealed record CommentAddedMessage(Guid CommentId, Guid CardId, Guid AuthorId, string Body);

public sealed record AttachmentAddedMessage(Guid AttachmentId, Guid CardId, string FileName);

public sealed record NotificationMessage(
    Guid NotificationId,
    NotificationType Type,
    string Message,
    Guid? ResourceId,
    DateTimeOffset CreatedAt);
