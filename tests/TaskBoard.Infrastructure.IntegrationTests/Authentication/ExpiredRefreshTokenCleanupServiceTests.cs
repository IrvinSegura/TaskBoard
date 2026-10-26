using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskBoard.Domain.Users;
using TaskBoard.Infrastructure.Authentication;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.IntegrationTests.Authentication;

public class ExpiredRefreshTokenCleanupServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task RunOnce_DeletesExpiredTokensAndKeepsRevokedOnesUntilTheyExpire()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"{Guid.NewGuid():N}@test.local", "User", "hash");
        var expired = RefreshToken.Create(user.Id, NewHash(), now.AddDays(-1));
        var active = RefreshToken.Create(user.Id, NewHash(), now.AddDays(1));
        var revoked = RefreshToken.Create(user.Id, NewHash(), now.AddDays(1));
        revoked.Revoke(now);

        await using (var db = fixture.CreateDbContext())
        {
            db.Users.Add(user);
            db.RefreshTokens.AddRange(expired, active, revoked);
            await db.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(fixture.ConnectionString));
        await using var provider = services.BuildServiceProvider();
        var cleanup = new ExpiredRefreshTokenCleanupService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new JwtOptions()),
            TimeProvider.System,
            NullLogger<ExpiredRefreshTokenCleanupService>.Instance);

        await cleanup.RunOnceAsync(CancellationToken.None);

        await using var check = fixture.CreateDbContext();
        var remaining = await check.RefreshTokens.Where(t => t.UserId == user.Id).Select(t => t.Id).ToListAsync();
        remaining.ShouldBe([active.Id, revoked.Id], ignoreOrder: true);
    }

    private static string NewHash() => Guid.NewGuid().ToString("N");
}
