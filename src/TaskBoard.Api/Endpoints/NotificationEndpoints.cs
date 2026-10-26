using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Notifications.GetMyNotifications;
using TaskBoard.Application.Features.Notifications.MarkNotificationAsRead;

namespace TaskBoard.Api.Endpoints;

internal sealed class NotificationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/notifications").WithTags("Notifications").RequireAuthorization();

        group.MapGet("/", async (
            IQueryHandler<GetMyNotificationsQuery, IReadOnlyList<NotificationResponse>> handler,
            CancellationToken cancellationToken,
            bool unreadOnly = false) =>
        {
            var result = await handler.Handle(new GetMyNotificationsQuery(unreadOnly), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapPatch("/{notificationId:guid}/read", async (
            Guid notificationId,
            ICommandHandler<MarkNotificationAsReadCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new MarkNotificationAsReadCommand(notificationId), cancellationToken);
            return result.ToHttpResult();
        });
    }
}
