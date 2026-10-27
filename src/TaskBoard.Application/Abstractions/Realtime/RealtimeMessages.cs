using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Abstractions.Realtime;

public sealed record BoardRenamedMessage(Guid BoardId, string Name);

public sealed record BoardArchivedMessage(Guid BoardId);

public sealed record BoardRestoredMessage(Guid BoardId);

public sealed record BoardDeletedMessage(Guid BoardId);

public sealed record BoardMemberRemovedMessage(Guid BoardId, Guid UserId);

public sealed record ColumnCreatedMessage(Guid ColumnId, string Name, double Position);

public sealed record ColumnRenamedMessage(Guid ColumnId, string Name);

public sealed record ColumnMovedMessage(Guid ColumnId, double Position);

public sealed record ColumnDeletedMessage(Guid ColumnId);

public sealed record CardCreatedMessage(Guid CardId, Guid ColumnId, string Title, double Position);

public sealed record CardUpdatedMessage(
    Guid CardId,
    string Title,
    string? Description,
    CardPriority Priority,
    DateTimeOffset? DueDate);

public sealed record CardMovedMessage(Guid CardId, Guid FromColumnId, Guid ToColumnId, double Position);

public sealed record CardArchivedMessage(Guid CardId);

public sealed record CardDeletedMessage(Guid CardId);

public sealed record CardRestoredMessage(Guid CardId, Guid ColumnId, string Title, double Position);

public sealed record CardAssignedMessage(Guid CardId, Guid UserId);

public sealed record CardUnassignedMessage(Guid CardId, Guid UserId);

public sealed record LabelsChangedMessage(Guid BoardId);

public sealed record CardLabelsChangedMessage(Guid CardId);

public sealed record ChecklistChangedMessage(Guid CardId);

public sealed record CommentAddedMessage(Guid CommentId, Guid CardId, Guid AuthorId, string Body);

public sealed record CommentEditedMessage(Guid CommentId, Guid CardId, string Body);

public sealed record CommentDeletedMessage(Guid CommentId, Guid CardId);

public sealed record AttachmentAddedMessage(Guid AttachmentId, Guid CardId, string FileName);

public sealed record AttachmentDeletedMessage(Guid AttachmentId, Guid CardId);

public sealed record TeamDeletedMessage(Guid TeamId, string Name);

public sealed record NotificationMessage(
    Guid NotificationId,
    NotificationType Type,
    string Message,
    Guid? ResourceId,
    DateTimeOffset CreatedAt);
