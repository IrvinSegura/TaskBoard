using System.Collections.Concurrent;
using TaskBoard.Application.Abstractions.Email;

namespace TaskBoard.Api.FunctionalTests;

/// <summary>
/// Takes the place of the real sender in these tests, so they can read what would have been sent.
/// </summary>
public sealed class RecordingEmailSender : IEmailSender
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<EmailMessage> To(string address) => [.. _messages.Where(m => m.To == address)];

    // Notification emails leave from the outbox, after the request has already answered.
    public async Task<EmailMessage> WaitForAsync(string address, string subjectPart)
    {
        var deadline = DateTime.UtcNow + Timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (To(address).FirstOrDefault(m => m.Subject.Contains(subjectPart)) is { } message)
            {
                return message;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"No email about '{subjectPart}' reached {address} within {Timeout}.");
    }
}
