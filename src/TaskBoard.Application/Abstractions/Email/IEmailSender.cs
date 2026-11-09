namespace TaskBoard.Application.Abstractions.Email;

/// <summary>
/// Takes an email for delivery. Delivery happens later and in the background, so a slow or failing
/// mail server never holds up a request or an outbox message.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
