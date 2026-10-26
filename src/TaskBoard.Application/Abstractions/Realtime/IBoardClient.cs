namespace TaskBoard.Application.Abstractions.Realtime;

/// <summary>
/// Messages the server pushes to connected clients. The method names are the event names the frontend listens to.
/// </summary>
public interface IBoardClient
{
    Task BoardRenamed(BoardRenamedMessage message);

    Task BoardArchived(BoardArchivedMessage message);

    Task BoardRestored(BoardRestoredMessage message);

    // Also reaches the removed user, whose client should leave the board when it sees its own id.
    Task BoardMemberRemoved(BoardMemberRemovedMessage message);

    Task ColumnCreated(ColumnCreatedMessage message);

    Task ColumnRenamed(ColumnRenamedMessage message);

    Task ColumnMoved(ColumnMovedMessage message);

    Task CardCreated(CardCreatedMessage message);

    Task CardUpdated(CardUpdatedMessage message);

    Task CardMoved(CardMovedMessage message);

    Task CardArchived(CardArchivedMessage message);

    Task CardRestored(CardRestoredMessage message);

    Task CardAssigned(CardAssignedMessage message);

    Task CardUnassigned(CardUnassignedMessage message);

    Task CommentAdded(CommentAddedMessage message);

    Task CommentEdited(CommentEditedMessage message);

    Task CommentDeleted(CommentDeletedMessage message);

    Task AttachmentAdded(AttachmentAddedMessage message);

    Task AttachmentDeleted(AttachmentDeletedMessage message);

    Task NotificationReceived(NotificationMessage message);
}
