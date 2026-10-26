using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace TaskBoard.Api.FunctionalTests;

/// <summary>
/// Runs the real API in memory against a throwaway PostgreSQL container (requires Docker).
/// </summary>
public sealed class TaskBoardApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" skips the development-only migration step in Program.cs.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "functional-tests-signing-key-0123456789abcdef");
    }

    public Task InitializeAsync() => _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await DisposeAsync();
    }
}
