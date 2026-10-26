using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Notifications.GetMyNotifications;

internal sealed class GetMyNotificationsQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetMyNotificationsQuery, IReadOnlyList<NotificationResponse>>
{
    private const int MaxItems = 50;

    public async Task<Result<IReadOnlyList<NotificationResponse>>> Handle(
        GetMyNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var notifications = db.Notifications.Where(n => n.UserId == userId);
        if (query.UnreadOnly)
        {
            notifications = notifications.Where(n => !n.IsRead);
        }

        var items = await notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(MaxItems)
            .Select(n => new NotificationResponse(n.Id, n.Type, n.Message, n.ResourceId, n.IsRead, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<NotificationResponse>>(items);
    }
}
