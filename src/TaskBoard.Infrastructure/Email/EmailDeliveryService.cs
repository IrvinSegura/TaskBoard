using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskBoard.Application.Abstractions.Email;

namespace TaskBoard.Infrastructure.Email;

/// <summary>
/// Delivers the queued emails one at a time, retrying a few times so a short mail server outage loses nothing.
/// </summary>
internal sealed class EmailDeliveryService(
    QueuedEmailSender sender,
    IEmailTransport transport,
    IOptions<EmailOptions> options,
    TimeProvider timeProvider,
    ILogger<EmailDeliveryService> logger)
    : BackgroundService
{
    private const int MaxAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in sender.Queue.ReadAllAsync(stoppingToken))
        {
            await DeliverAsync(message, stoppingToken);
        }
    }

    internal async Task DeliverAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await transport.SendAsync(message, cancellationToken);
                return;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // The address and the body are personal data, and the body may hold a reset link: neither is logged.
                if (attempt == MaxAttempts)
                {
                    logger.LogError(ex, "Giving up on the email \"{Subject}\" after {Attempts} attempts", message.Subject, attempt);
                    return;
                }

                logger.LogWarning(ex, "Attempt {Attempt} to send the email \"{Subject}\" failed", attempt, message.Subject);
                await Task.Delay(options.Value.RetryDelay * attempt, timeProvider, cancellationToken);
            }
        }
    }
}
