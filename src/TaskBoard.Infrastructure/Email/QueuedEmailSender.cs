using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Email;

namespace TaskBoard.Infrastructure.Email;

/// <summary>
/// Leaves emails in a queue for <see cref="EmailDeliveryService"/>. The queue lives in memory: emails still in it
/// when the API stops are lost, which is acceptable for notices that are also in the app and for reset links
/// that can be requested again.
/// </summary>
internal sealed class QueuedEmailSender(ILogger<QueuedEmailSender> logger) : IEmailSender
{
    private const int Capacity = 1000;

    private readonly Channel<EmailMessage> _queue = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(Capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });

    public ChannelReader<EmailMessage> Queue => _queue.Reader;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        // Full means the mail server has been failing for a while. Better to lose an email than to hold up requests.
        if (!_queue.Writer.TryWrite(message))
        {
            logger.LogError("The email queue is full; dropping \"{Subject}\"", message.Subject);
        }

        return Task.CompletedTask;
    }
}
