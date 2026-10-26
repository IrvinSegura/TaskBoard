using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskBoard.Application.Abstractions.Storage;
using TaskBoard.Domain.Attachments;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Users;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Storage;

namespace TaskBoard.Infrastructure.IntegrationTests.Storage;

public sealed class OrphanedFileCleanupServiceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>, IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), "taskboard-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task RunOnce_DeletesOnlyOldFilesWithoutAnAttachmentRow()
    {
        var options = Options.Create(new FileStorageOptions
        {
            LocalRootPath = _rootPath,
            OrphanGracePeriod = TimeSpan.FromHours(1)
        });
        var storage = new LocalFileStorage(options, TimeProvider.System);

        var referenced = await UploadAsync(storage, age: TimeSpan.FromHours(2));
        var oldOrphan = await UploadAsync(storage, age: TimeSpan.FromHours(2));
        var recentOrphan = await UploadAsync(storage, age: TimeSpan.Zero);
        await SaveAttachmentAsync(referenced);

        await using var services = BuildServices(storage);
        var cleanup = new OrphanedFileCleanupService(
            services.GetRequiredService<IServiceScopeFactory>(),
            options,
            TimeProvider.System,
            NullLogger<OrphanedFileCleanupService>.Instance);

        var deleted = await cleanup.RunOnceAsync(CancellationToken.None);

        deleted.ShouldBe(1);
        (await ExistsAsync(storage, referenced)).ShouldBeTrue();
        (await ExistsAsync(storage, oldOrphan)).ShouldBeFalse();
        // Too young to tell apart from an upload whose row is still being saved.
        (await ExistsAsync(storage, recentOrphan)).ShouldBeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private ServiceProvider BuildServices(IFileStorage storage)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(fixture.ConnectionString));
        services.AddSingleton(storage);
        return services.BuildServiceProvider();
    }

    private async Task<string> UploadAsync(LocalFileStorage storage, TimeSpan age)
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("file content"));
        var storageKey = await storage.UploadAsync(content, "text/plain", CancellationToken.None);

        File.SetLastWriteTimeUtc(Path.Combine(_rootPath, storageKey), DateTime.UtcNow - age);
        return storageKey;
    }

    private static async Task<bool> ExistsAsync(IFileStorage storage, string storageKey)
    {
        await using var stream = await storage.OpenReadAsync(storageKey, CancellationToken.None);
        return stream is not null;
    }

    private async Task SaveAttachmentAsync(string storageKey)
    {
        var owner = User.Create($"{Guid.NewGuid():N}@test.local", "Owner", "hash");
        var team = Team.Create("Team", owner.Id);
        var board = Board.Create(team.Id, "Board", owner.Id, BoardVisibility.Private);
        var column = board.AddColumn("To do").Value;
        var card = Card.Create(board.Id, column.Id, "Card", null, owner.Id, 1024);
        var attachment = Attachment.Create(card.Id, board.Id, owner.Id, "notes.txt", "text/plain", 12, storageKey);

        await using var db = fixture.CreateDbContext();
        db.Users.Add(owner);
        db.Teams.Add(team);
        db.Boards.Add(board);
        db.Cards.Add(card);
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync();
    }
}
