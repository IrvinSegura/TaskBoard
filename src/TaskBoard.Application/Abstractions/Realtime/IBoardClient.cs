namespace TaskBoard.Application.Abstractions.Realtime;

/// <summary>
/// Messages the server pushes to connected clients. The method names are the event names the frontend listens to.
/// </summary>
public interface IBoardClient
{
    Task ColumnCreated(ColumnCreatedMessage message);

    Task CardCreated(CardCreatedMessage message);

    Task CardMoved(CardMovedMessage message);

    Task CardAssigned(CardAssignedMessage message);

    Task CommentAdded(CommentAddedMessage message);

    Task AttachmentAdded(AttachmentAddedMessage message);

    Task NotificationReceived(NotificationMessage message);
}
