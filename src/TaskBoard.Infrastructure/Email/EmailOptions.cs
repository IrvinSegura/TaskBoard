using MailKit.Security;

namespace TaskBoard.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    // Off by default: until a mail server is configured, emails only reach the log.
    public bool Enabled { get; init; }

    // The web app's address, for the links in the emails.
    public string WebAppUrl { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = "TaskBoard";

    // How long the first retry waits; each later one waits longer.
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(5);

    public SmtpOptions Smtp { get; init; } = new();

    public sealed class SmtpOptions
    {
        public string Host { get; init; } = string.Empty;

        public int Port { get; init; } = 587;

        public SecureSocketOptions Security { get; init; } = SecureSocketOptions.StartTls;

        public string Username { get; init; } = string.Empty;

        // Never stored in appsettings: it comes from the Email__Smtp__Password environment variable (or user-secrets).
        public string Password { get; init; } = string.Empty;

        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    }
}
