using System.Text;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Infrastructure.Email;

namespace TaskBoard.Infrastructure.IntegrationTests.Email;

public class SmtpEmailTransportTests
{
    private static readonly EmailMessage Message =
        new("ana@example.com", "Te asignaron la tarjeta «Demo»", "Hola Ana,", "<p>Hola Ana,</p>");

    private static SmtpEmailTransport CreateTransport(int port, string username = "") =>
        new(Options.Create(new EmailOptions
        {
            Enabled = true,
            FromAddress = "avisos@taskboard.test",
            FromName = "TaskBoard",
            Smtp = new EmailOptions.SmtpOptions
            {
                Host = "127.0.0.1",
                Port = port,
                Security = SecureSocketOptions.None,
                Username = username,
                Password = username.Length > 0 ? "secret" : string.Empty,
                Timeout = TimeSpan.FromSeconds(5)
            }
        }));

    [Fact]
    public async Task SendAsync_DeliversAMultipartMessageFromTheConfiguredAddress()
    {
        await using var server = new FakeSmtpServer();

        await CreateTransport(server.Port).SendAsync(Message, CancellationToken.None);

        server.Commands.ShouldContain(c => c.StartsWith("RCPT TO:<ana@example.com>", StringComparison.OrdinalIgnoreCase));
        server.Commands.ShouldNotContain(c => c.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase));

        using var data = new MemoryStream(Encoding.ASCII.GetBytes(server.Data!));
        var received = await MimeMessage.LoadAsync(data);

        received.Subject.ShouldBe(Message.Subject);
        received.From.Mailboxes.ShouldHaveSingleItem().Address.ShouldBe("avisos@taskboard.test");
        received.From.Mailboxes.Single().Name.ShouldBe("TaskBoard");
        received.Body.ContentType.MimeType.ShouldBe("multipart/alternative");
        received.TextBody.TrimEnd().ShouldBe(Message.TextBody);
        received.HtmlBody.TrimEnd().ShouldBe(Message.HtmlBody);
    }

    [Fact]
    public async Task SendAsync_WithCredentials_SignsIn()
    {
        await using var server = new FakeSmtpServer();

        await CreateTransport(server.Port, username: "smtp-user").SendAsync(Message, CancellationToken.None);

        server.Commands.ShouldContain(c => c.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase));
    }
}
