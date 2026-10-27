using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Notifications.DeleteNotification;

public sealed record DeleteNotificationCommand(Guid NotificationId) : ICommand;
