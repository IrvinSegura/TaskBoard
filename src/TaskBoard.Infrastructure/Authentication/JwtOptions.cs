using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = null!;

    [Required]
    public string Audience { get; init; } = null!;

    // HMAC-SHA256 needs at least 256 bits. Keep the real key in user-secrets or environment variables.
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = null!;

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromHours(1);
}
