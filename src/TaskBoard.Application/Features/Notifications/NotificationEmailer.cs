using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Notifications;

/// <summary>
/// Sends a notification's copy by email, if the user wants it.
/// </summary>
internal sealed class NotificationEmailer(
    IUserRepository users,
    NotificationEmails emails,
    IEmailSender sender,
    ILogger<NotificationEmailer> logger)
{
    public async Task SendAsync(
        Guid userId,
        NotificationType type,
        NotificationEmailFacts facts,
        CancellationToken cancellationToken)
    {
        // The notification is already stored and pushed; an email problem must not fail it,
        // or stop the rest of the people a comment notifies.
        try
        {
            var user = await users.GetByIdAsync(userId, cancellationToken);
            if (user is null || !user.WantsEmailFor(type))
            {
                return;
            }

            var message = emails.Create(type, user.Email, user.DisplayName, facts);
            await sender.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not send the {Type} email to user {UserId}", type, userId);
        }
    }
}
