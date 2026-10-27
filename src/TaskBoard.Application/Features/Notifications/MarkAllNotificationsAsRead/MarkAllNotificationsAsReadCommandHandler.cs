using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Notifications.MarkAllNotificationsAsRead;

internal sealed class MarkAllNotificationsAsReadCommandHandler(
    INotificationRepository notifications,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<MarkAllNotificationsAsReadCommand>
{
    public async Task<Result> Handle(MarkAllNotificationsAsReadCommand command, CancellationToken cancellationToken)
    {
        var unread = await notifications.GetUnreadForUserAsync(currentUser.UserId, cancellationToken);
        if (unread.Count == 0)
        {
            return Result.Success();
        }

        var readAt = timeProvider.GetUtcNow();
        foreach (var notification in unread)
        {
            notification.MarkAsRead(readAt);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
