using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Notifications;

/// <summary>
/// Stores a notification, pushes it to the user's open connections and sends its copy by email.
/// Used by domain event handlers.
/// </summary>
internal sealed class NotificationPublisher(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    IRealtimeNotifier notifier,
    NotificationEmailer emailer)
{
    public async Task PublishAsync(
        Guid userId,
        NotificationType type,
        string message,
        Guid? resourceId,
        NotificationEmailFacts email,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(userId, type, message, resourceId);
        notifications.Add(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await notifier.User(userId).NotificationReceived(
            new NotificationMessage(notification.Id, type, message, resourceId, notification.CreatedAt));

        await emailer.SendAsync(userId, type, email, cancellationToken);
    }
}
