using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Notifications.DeleteNotification;

internal sealed class DeleteNotificationCommandHandler(
    INotificationRepository notifications,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteNotificationCommand>
{
    public async Task<Result> Handle(DeleteNotificationCommand command, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetByIdAsync(command.NotificationId, cancellationToken);

        // Other users' notifications look exactly like missing ones.
        if (notification is null || notification.UserId != currentUser.UserId)
        {
            return NotificationErrors.NotFound(command.NotificationId);
        }

        notifications.Remove(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
