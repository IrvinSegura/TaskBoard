using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskBoard.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace TaskBoard.Infrastructure.IntegrationTests;

/// <summary>
/// One real PostgreSQL container per test class (requires Docker).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();

    public string ConnectionString => _container.GetConnectionString();

    public ApplicationDbContext CreateDbContext(params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(interceptors)
            .Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // EnsureCreated keeps tests independent of migrations; switch to MigrateAsync once migrations exist.
        await using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
