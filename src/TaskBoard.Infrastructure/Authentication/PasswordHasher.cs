using Microsoft.AspNetCore.Identity;
using TaskBoard.Application.Abstractions.Authentication;

namespace TaskBoard.Infrastructure.Authentication;

/// <summary>
/// Wraps ASP.NET Core Identity's hasher (PBKDF2, salted, versioned) without pulling in the rest of Identity.
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<object> Hasher = new();

    // The Identity hasher ignores the user instance; any object works.
    private static readonly object User = new();

    public string Hash(string password) => Hasher.HashPassword(User, password);

    public bool Verify(string password, string passwordHash) =>
        Hasher.VerifyHashedPassword(User, passwordHash, password) != PasswordVerificationResult.Failed;
}
