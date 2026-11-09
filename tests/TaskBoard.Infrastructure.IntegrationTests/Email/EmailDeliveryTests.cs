using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Infrastructure.Email;

namespace TaskBoard.Infrastructure.IntegrationTests.Email;

public class EmailDeliveryTests
{
    private static readonly EmailMessage Message = new(
        "ana@example.com", "Restablece tu contraseña", "Enlace: https://app.test/reset-password#token=secret", "<p>Enlace</p>");

    private static EmailDeliveryService CreateService(QueuedEmailSender sender, IEmailTransport transport) =>
        new(sender, transport, Options.Create(new EmailOptions { RetryDelay = TimeSpan.Zero }),
            TimeProvider.System, NullLogger<EmailDeliveryService>.Instance);

    private static QueuedEmailSender CreateSender() => new(NullLogger<QueuedEmailSender>.Instance);

    [Fact]
    public async Task QueuedEmail_ReachesTheTransportInTheBackground()
    {
        var sender = CreateSender();
        var transport = new FakeTransport();
        var service = CreateService(sender, transport);

        await service.StartAsync(CancellationToken.None);
        await sender.SendAsync(Message, CancellationToken.None);

        (await transport.Delivered.Task.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(Message);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Deliver_AfterAFailure_TriesAgain()
    {
        var transport = new FakeTransport { FailuresBeforeSuccess = 2 };

        await CreateService(CreateSender(), transport).DeliverAsync(Message, CancellationToken.None);

        transport.Attempts.ShouldBe(3);
        transport.Delivered.Task.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task Deliver_WhenItKeepsFailing_GivesUpWithoutThrowing()
    {
        var transport = new FakeTransport { FailuresBeforeSuccess = int.MaxValue };

        await Should.NotThrowAsync(() =>
            CreateService(CreateSender(), transport).DeliverAsync(Message, CancellationToken.None));

        transport.Attempts.ShouldBe(3);
    }

    [Fact]
    public async Task SendAsync_WhenTheQueueIsFull_DropsTheEmailInsteadOfWaiting()
    {
        var sender = CreateSender();

        var sends = Enumerable.Range(0, 1001).Select(_ => sender.SendAsync(Message, CancellationToken.None)).ToList();

        sends.ShouldAllBe(send => send.IsCompletedSuccessfully);
        await Task.WhenAll(sends);
        sender.Queue.Count.ShouldBe(1000);
    }

    [Fact]
    public void LoggingTransport_InDevelopment_LogsTheWholeTextSoTheLinkCanBeFollowed()
    {
        var log = LogOf(Environments.Development);

        log.ShouldContain("ana@example.com");
        log.ShouldContain("#token=secret");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void LoggingTransport_AnywhereElse_LogsOnlyTheSubject(string environment)
    {
        var log = LogOf(environment);

        log.ShouldContain("Restablece tu contraseña");
        log.ShouldNotContain("ana@example.com");
        log.ShouldNotContain("secret");
    }

    private static string LogOf(string environment)
    {
        var logger = new CapturingLogger();
        var hostEnvironment = new FakeHostEnvironment { EnvironmentName = environment };

        new LoggingEmailTransport(hostEnvironment, logger).SendAsync(Message, CancellationToken.None);

        return logger.Text;
    }

    private sealed class FakeTransport : IEmailTransport
    {
        public int FailuresBeforeSuccess { get; init; }

        public int Attempts { get; private set; }

        public TaskCompletionSource<EmailMessage> Delivered { get; } = new();

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Attempts++;
            if (Attempts <= FailuresBeforeSuccess)
            {
                throw new IOException("The mail server is down.");
            }

            Delivered.TrySetResult(message);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingLogger : ILogger<LoggingEmailTransport>
    {
        public string Text { get; private set; } = string.Empty;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Text += formatter(state, exception);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;

        public string ApplicationName { get; set; } = string.Empty;

        public string ContentRootPath { get; set; } = string.Empty;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
