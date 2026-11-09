namespace TaskBoard.Application.Abstractions.Authentication;

/// <summary>
/// The token in the link that confirms a user's email.
/// </summary>
public interface IEmailVerificationTokens
{
    TimeSpan ValidFor { get; }

    string Create(Guid userId);

    // The user it was made for, or null when it was tampered with or has expired.
    Guid? Read(string token);
}
