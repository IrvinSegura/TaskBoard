using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace TaskBoard.Api.FunctionalTests;

/// <summary>
/// Runs the real API in memory against a throwaway PostgreSQL container (requires Docker).
/// </summary>
public sealed class TaskBoardApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Also used by tests that forge tokens, such as an already expired one.
    public const string SigningKey = "functional-tests-signing-key-0123456789abcdef";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    // Uploaded files go to a throwaway folder instead of the API project's storage folder.
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), "taskboard-tests", Guid.NewGuid().ToString("N"));

    // What the API would have sent, for tests to read.
    public RecordingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" skips the development-only migration step in Program.cs; InitializeAsync migrates instead.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("FileStorage:Provider", "Local");
        builder.UseSetting("FileStorage:LocalRootPath", _storagePath);

        // Every test signs up its own users from the same address; the rate limit has its own test.
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "100000");

        builder.UseSetting("Email:WebAppUrl", "https://taskboard.test");

        // Tests send the reminders themselves, so a timer never sends one in the middle of another test.
        builder.UseSetting("DueReminders:Interval", "00:00:00");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Same migrations as production, so the tests also catch a broken migration.
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await DisposeAsync();

        if (Directory.Exists(_storagePath))
        {
            Directory.Delete(_storagePath, recursive: true);
        }
    }
}
