using TaskBoard.Application.Abstractions.Messaging;

namespace TaskBoard.Application.Features.Notifications.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId) : ICommand;
