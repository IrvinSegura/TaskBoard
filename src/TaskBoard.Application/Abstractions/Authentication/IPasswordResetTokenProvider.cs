namespace TaskBoard.Application.Abstractions.Authentication;

public interface IPasswordResetTokenProvider
{
    // A random opaque token and the hash to store; the plain value only ever travels in the email.
    GeneratedPasswordResetToken Create();

    string Hash(string token);
}

public sealed record GeneratedPasswordResetToken(string Token, string Hash, DateTimeOffset ExpiresAt, TimeSpan ValidFor);
