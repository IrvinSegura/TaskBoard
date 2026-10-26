namespace TaskBoard.Api.RateLimiting;

/// <summary>
/// Attempts allowed per client IP on the sign-in and registration endpoints, against password guessing.
/// </summary>
public sealed class AuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:Auth";

    public const string PolicyName = "auth";

    public int PermitLimit { get; init; } = 10;

    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
}
