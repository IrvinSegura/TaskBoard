using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(ApplicationDbContext db) : INotificationRepository
{
    public async Task<Notification?> GetByIdAsync(Guid notificationId, CancellationToken cancellationToken) =>
        await db.Notifications.FindAsync([notificationId], cancellationToken);

    public void Add(Notification notification) => db.Notifications.Add(notification);
}
