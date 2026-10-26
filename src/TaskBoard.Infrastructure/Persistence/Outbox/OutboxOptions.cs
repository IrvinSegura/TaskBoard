namespace TaskBoard.Infrastructure.Persistence.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    // Short by default: this is the extra latency of realtime updates.
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    public int BatchSize { get; init; } = 50;
}
