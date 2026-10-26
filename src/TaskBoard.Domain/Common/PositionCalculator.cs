namespace TaskBoard.Domain.Common;

/// <summary>
/// Fractional ordering for columns and cards: moving an item only rewrites its own position.
/// </summary>
public static class PositionCalculator
{
    public const double Step = 1024d;

    public static double After(double? last) => (last ?? 0d) + Step;

    // TODO: precision runs out after ~50 splits between the same neighbours; add a rebalance job then.
    public static double Between(double? previous, double? next) => (previous, next) switch
    {
        (null, null) => Step,
        (null, { } n) => n / 2,
        ({ } p, null) => p + Step,
        ({ } p, { } n) => (p + n) / 2
    };
}
