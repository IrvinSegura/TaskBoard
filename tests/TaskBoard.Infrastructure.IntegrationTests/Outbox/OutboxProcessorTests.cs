using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards.Events;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Persistence.Outbox;

namespace TaskBoard.Infrastructure.IntegrationTests.Outbox;

public class OutboxProcessorTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task ConcurrentProcessors_HandleEveryMessageExactlyOnce()
    {
        // Each test uses its own board id, so leftovers from other tests in the class are ignored.
        var boardId = Guid.NewGuid();
        var handler = new RecordingHandler(boardId, TimeSpan.FromMilliseconds(10));
        await using var services = BuildServices(handler);
        await InsertMessagesAsync(Enumerable.Range(0, 30).Select(i => new BoardRenamedEvent(boardId, $"Name {i}")));

        // Like three API instances polling the same table with small batches.
        var processors = Enumerable.Range(0, 3).Select(_ => CreateProcessor(services, batchSize: 4)).ToList();
        await Task.WhenAll(processors.Select(DrainAsync));

        handler.Handled.Count.ShouldBe(30);
        handler.Handled.Distinct().Count().ShouldBe(30);
        await using var db = fixture.CreateDbContext();
        var pending = await db.OutboxMessages.Where(m => m.ProcessedAt == null).ToListAsync();
        pending.ShouldNotContain(m => m.Content.Contains(boardId.ToString()));
    }

    [Fact]
    public async Task ClaimedMessages_AreRetriedOnlyAfterTheirLeaseExpires()
    {
        var boardId = Guid.NewGuid();
        var handler = new RecordingHandler(boardId, TimeSpan.Zero);
        await using var services = BuildServices(handler);
        var now = DateTimeOffset.UtcNow;

        // One left behind by an instance that died, one still held by a live instance.
        await InsertMessagesAsync([new BoardRenamedEvent(boardId, "Abandoned")], lockedUntil: now.AddMinutes(-1));
        await InsertMessagesAsync([new BoardRenamedEvent(boardId, "Held")], lockedUntil: now.AddMinutes(10));

        await DrainAsync(CreateProcessor(services, batchSize: 10));

        handler.Handled.ShouldBe(["Abandoned"]);
    }

    private ServiceProvider BuildServices(RecordingHandler handler)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(fixture.ConnectionString));
        services.AddScoped<DomainEventDispatcher>();
        services.AddSingleton<IDomainEventHandler<BoardRenamedEvent>>(handler);
        return services.BuildServiceProvider();
    }

    private static OutboxProcessor CreateProcessor(ServiceProvider services, int batchSize) =>
        new(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxOptions { BatchSize = batchSize }),
            TimeProvider.System,
            NullLogger<OutboxProcessor>.Instance);

    private static async Task DrainAsync(OutboxProcessor processor)
    {
        while (await processor.ProcessBatchAsync(CancellationToken.None) > 0)
        {
        }
    }

    private async Task InsertMessagesAsync(IEnumerable<BoardRenamedEvent> events, DateTimeOffset? lockedUntil = null)
    {
        await using var db = fixture.CreateDbContext();
        foreach (var domainEvent in events)
        {
            var message = OutboxMessage.Create(domainEvent, DateTimeOffset.UtcNow);
            message.LockedUntil = lockedUntil;
            db.OutboxMessages.Add(message);
        }

        await db.SaveChangesAsync();
    }

    private sealed class RecordingHandler(Guid boardId, TimeSpan delay) : IDomainEventHandler<BoardRenamedEvent>
    {
        public ConcurrentQueue<string> Handled { get; } = new();

        public async Task Handle(BoardRenamedEvent domainEvent, CancellationToken cancellationToken)
        {
            if (domainEvent.BoardId != boardId)
            {
                return;
            }

            await Task.Delay(delay, cancellationToken);
            Handled.Enqueue(domainEvent.Name);
        }
    }
}
