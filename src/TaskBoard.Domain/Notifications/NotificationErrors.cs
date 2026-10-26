using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Notifications;

public static class NotificationErrors
{
    public static Error NotFound(Guid notificationId) =>
        Error.NotFound("Notifications.NotFound", $"Notification '{notificationId}' was not found.");
}
