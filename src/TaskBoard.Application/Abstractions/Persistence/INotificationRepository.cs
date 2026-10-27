using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(Guid userId, CancellationToken cancellationToken);

    void Add(Notification notification);

    void Remove(Notification notification);
}
