using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Notifications;

/// <summary>
/// Stores a notification and pushes it to the user's open connections. Used by domain event handlers.
/// </summary>
internal sealed class NotificationPublisher(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    IRealtimeNotifier notifier)
{
    public async Task PublishAsync(
        Guid userId,
        NotificationType type,
        string message,
        Guid? resourceId,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(userId, type, message, resourceId);
        notifications.Add(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await notifier.User(userId).NotificationReceived(
            new NotificationMessage(notification.Id, type, message, resourceId, notification.CreatedAt));
    }
}
