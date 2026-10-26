using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Notifications.MarkNotificationAsRead;

internal sealed class MarkNotificationAsReadCommandHandler(
    INotificationRepository notifications,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<MarkNotificationAsReadCommand>
{
    public async Task<Result> Handle(MarkNotificationAsReadCommand command, CancellationToken cancellationToken)
    {
        var notification = await notifications.GetByIdAsync(command.NotificationId, cancellationToken);

        // Other users' notifications look exactly like missing ones.
        if (notification is null || notification.UserId != currentUser.UserId)
        {
            return NotificationErrors.NotFound(command.NotificationId);
        }

        notification.MarkAsRead(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
