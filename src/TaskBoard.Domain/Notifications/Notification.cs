using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Notifications;

public sealed class Notification : Entity
{
    private Notification()
    {
    }

    public Guid UserId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Message { get; private set; } = null!;

    // Card, board or team the notification points to, so the client can navigate there.
    public Guid? ResourceId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public static Notification Create(Guid userId, NotificationType type, string message, Guid? resourceId) => new()
    {
        UserId = userId,
        Type = type,
        Message = message,
        ResourceId = resourceId
    };

    public void MarkAsRead(DateTimeOffset readAt)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = readAt;
    }
}
