namespace TaskBoard.Application.Abstractions.Realtime;

/// <summary>
/// Messages the server pushes to connected clients. The method names are the event names the frontend listens to.
/// </summary>
public interface IBoardClient
{
    Task BoardRenamed(BoardRenamedMessage message);

    Task BoardArchived(BoardArchivedMessage message);

    Task BoardRestored(BoardRestoredMessage message);

    // The board is gone; clients viewing it should leave.
    Task BoardDeleted(BoardDeletedMessage message);

    // Also reaches a user who lost access, whose client should leave the board when it sees its own id.
    // That user can get it twice (direct notice and group message), so handle it idempotently.
    Task BoardMemberRemoved(BoardMemberRemovedMessage message);

    Task ColumnCreated(ColumnCreatedMessage message);

    Task ColumnRenamed(ColumnRenamedMessage message);

    Task ColumnMoved(ColumnMovedMessage message);

    // Its cards are gone with it.
    Task ColumnDeleted(ColumnDeletedMessage message);

    Task CardCreated(CardCreatedMessage message);

    Task CardUpdated(CardUpdatedMessage message);

    Task CardMoved(CardMovedMessage message);

    Task CardArchived(CardArchivedMessage message);

    Task CardRestored(CardRestoredMessage message);

    Task CardDeleted(CardDeletedMessage message);

    Task CardAssigned(CardAssignedMessage message);

    Task CardUnassigned(CardUnassignedMessage message);

    // A board label was created, renamed, recoloured or deleted.
    Task LabelsChanged(LabelsChangedMessage message);

    Task CardLabelsChanged(CardLabelsChangedMessage message);

    // An item of the card's checklist was added, edited, checked off or deleted.
    Task ChecklistChanged(ChecklistChangedMessage message);

    Task CommentAdded(CommentAddedMessage message);

    Task CommentEdited(CommentEditedMessage message);

    Task CommentDeleted(CommentDeletedMessage message);

    Task AttachmentAdded(AttachmentAddedMessage message);

    Task AttachmentDeleted(AttachmentDeletedMessage message);

    Task NotificationReceived(NotificationMessage message);

    // Sent to every member of a deleted team, wherever they are in the app.
    Task TeamDeleted(TeamDeletedMessage message);

    // Chat events go to everyone who can open the conversation, wherever they are in the app.
    Task ChatMessageReceived(ChatMessageReceivedMessage message);

    Task ChatMessageUpdated(ChatMessageUpdatedMessage message);

    // The message stays as "deleted", without its text and files.
    Task ChatMessageDeleted(ChatMessageDeletedMessage message);

    // A group was renamed or changed participants; also reaches the people who just left it.
    Task ConversationUpdated(ConversationUpdatedMessage message);

    // Only to the reader, so their other tabs clear the unread count too.
    Task ConversationRead(ConversationReadMessage message);
}
