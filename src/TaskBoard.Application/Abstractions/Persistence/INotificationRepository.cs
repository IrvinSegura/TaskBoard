using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken);

    void Add(Notification notification);
}
