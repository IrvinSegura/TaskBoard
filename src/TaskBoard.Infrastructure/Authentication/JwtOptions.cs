using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;

    // Never stored in appsettings: it comes from the Jwt__SigningKey environment variable (or user-secrets).
    // A missing key is reported at startup by AddAuthenticationServices. HMAC-SHA256 needs at least 256 bits.
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters (256 bits) long.")]
    public string SigningKey { get; init; } = null!;

    // Short-lived: clients renew it with the refresh token instead of signing in again.
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);

    // How often expired refresh tokens are deleted. Zero turns the cleanup off.
    public TimeSpan RefreshTokenCleanupInterval { get; init; } = TimeSpan.FromDays(1);
}
