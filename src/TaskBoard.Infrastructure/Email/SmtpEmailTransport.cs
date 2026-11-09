using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using TaskBoard.Application.Abstractions.Email;

namespace TaskBoard.Infrastructure.Email;

// One connection per email: few are sent, and an idle connection to a mail server does not last anyway.
internal sealed class SmtpEmailTransport(IOptions<EmailOptions> options) : IEmailTransport
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var smtp = settings.Smtp;

        using var client = new SmtpClient { Timeout = (int)smtp.Timeout.TotalMilliseconds };
        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.Security, cancellationToken);

        if (!string.IsNullOrEmpty(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
        }

        await client.SendAsync(ToMimeMessage(message, settings), cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }

    private static MimeMessage ToMimeMessage(EmailMessage message, EmailOptions settings)
    {
        var mime = new MimeMessage { Subject = message.Subject };
        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));

        // multipart/alternative, with the text form first: clients show the last form they can render.
        mime.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();

        return mime;
    }
}
