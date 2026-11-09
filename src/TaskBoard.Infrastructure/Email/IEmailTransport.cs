using TaskBoard.Application.Abstractions.Email;

namespace TaskBoard.Infrastructure.Email;

// How a queued email actually leaves: by SMTP, or to the log while email is turned off.
internal interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
