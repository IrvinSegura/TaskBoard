namespace TaskBoard.Infrastructure.Authentication;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(1);
}
