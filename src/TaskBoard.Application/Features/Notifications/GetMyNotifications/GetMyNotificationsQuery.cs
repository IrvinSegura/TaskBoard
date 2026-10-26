using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Notifications.GetMyNotifications;

public sealed record GetMyNotificationsQuery(bool UnreadOnly) : IQuery<IReadOnlyList<NotificationResponse>>;

public sealed record NotificationResponse(
    Guid Id,
    NotificationType Type,
    string Message,
    Guid? ResourceId,
    bool IsRead,
    DateTimeOffset CreatedAt);
