namespace TaskBoard.Domain.Common;

/// <summary>
/// Fractional ordering for columns and cards: moving an item only rewrites its own position.
/// Repeated splits between the same neighbours halve the gap each time, so once it gets too small
/// the caller renumbers the whole column or board (<see cref="NeedsRebalance"/>, <see cref="Sequential"/>).
/// </summary>
public static class PositionCalculator
{
    public const double Step = 1024d;

    // Reached after about 30 splits from Step. A double still separates positions this close
    // up to magnitudes around 1e9, far beyond any real column, so midpoints never collapse.
    public const double MinGap = 1e-6;

    public static double After(double? last) => (last ?? 0d) + Step;

    public static bool NeedsRebalance(double? previous, double? next) => (previous, next) switch
    {
        ({ } p, { } n) => Math.Abs(n - p) < MinGap,
        (null, { } n) => n < MinGap,
        _ => false
    };

    // Evenly spaced position for the item at this index when renumbering.
    public static double Sequential(int index) => (index + 1) * Step;

    public static double Between(double? previous, double? next) => (previous, next) switch
    {
        (null, null) => Step,
        (null, { } n) => n / 2,
        ({ } p, null) => p + Step,
        ({ } p, { } n) => (p + n) / 2
    };
}
