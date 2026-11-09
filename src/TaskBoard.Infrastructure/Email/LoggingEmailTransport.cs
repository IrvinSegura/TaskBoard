using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Email;

namespace TaskBoard.Infrastructure.Email;

/// <summary>
/// For when email is turned off. In development it logs the whole text, so a reset link can be followed
/// without a mail server. Anywhere else only the subject: addresses and bodies are personal data, and a
/// body may hold a reset link.
/// </summary>
internal sealed class LoggingEmailTransport(IHostEnvironment environment, ILogger<LoggingEmailTransport> logger)
    : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (environment.IsDevelopment())
        {
            logger.LogInformation(
                "Email delivery is off. Email to {To}: \"{Subject}\"\n{Body}", message.To, message.Subject, message.TextBody);
        }
        else
        {
            logger.LogInformation("Email delivery is off; not sending \"{Subject}\"", message.Subject);
        }

        return Task.CompletedTask;
    }
}
