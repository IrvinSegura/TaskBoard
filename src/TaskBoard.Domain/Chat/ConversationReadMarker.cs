using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Chat;

/// <summary>
/// Up to when one user has read one conversation. Kept apart from the participants because board and team
/// conversations have none, and their members still need unread counts.
/// </summary>
public sealed class ConversationReadMarker : Entity
{
    private ConversationReadMarker()
    {
    }

    public Guid ConversationId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset LastReadAt { get; private set; }

    public static ConversationReadMarker Create(Guid conversationId, Guid userId, DateTimeOffset readAt) => new()
    {
        ConversationId = conversationId,
        UserId = userId,
        LastReadAt = readAt
    };

    // Never moves back, so a slow request from another tab cannot mark read messages as unread again.
    public void MarkRead(DateTimeOffset readAt)
    {
        if (readAt > LastReadAt)
        {
            LastReadAt = readAt;
        }
    }
}
