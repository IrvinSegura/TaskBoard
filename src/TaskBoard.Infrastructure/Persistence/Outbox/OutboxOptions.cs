namespace TaskBoard.Infrastructure.Persistence.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    // Short by default: this is the extra latency of realtime updates.
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    public int BatchSize { get; init; } = 50;

    // How long a claimed batch stays reserved for one instance. If that instance dies first,
    // another one picks the batch up once the lease runs out, so keep it above the time a batch takes.
    public TimeSpan LockDuration { get; init; } = TimeSpan.FromMinutes(2);
}
