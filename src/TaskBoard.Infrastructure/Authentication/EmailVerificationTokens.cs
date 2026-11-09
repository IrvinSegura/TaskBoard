using System.Buffers.Binary;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TaskBoard.Application.Abstractions.Authentication;

namespace TaskBoard.Infrastructure.Authentication;

/// <summary>
/// Signed rather than stored: confirming an email twice is harmless, so there is nothing to revoke or clean up.
/// The token is the user id and the expiry, followed by their HMAC-SHA256.
/// </summary>
internal sealed class EmailVerificationTokens(IOptions<JwtOptions> options, TimeProvider timeProvider)
    : IEmailVerificationTokens
{
    private const int PayloadLength = 24;
    private const int MacLength = 32;

    // Derived from the signing key, so a token for one purpose never passes as one for another.
    private readonly byte[] _key = HMACSHA256.HashData(
        Encoding.UTF8.GetBytes(options.Value.SigningKey), "taskboard-email-verification"u8.ToArray());

    public TimeSpan ValidFor { get; } = TimeSpan.FromDays(7);

    public string Create(Guid userId)
    {
        var payload = new byte[PayloadLength];
        userId.TryWriteBytes(payload);
        BinaryPrimitives.WriteInt64BigEndian(
            payload.AsSpan(16), timeProvider.GetUtcNow().Add(ValidFor).ToUnixTimeSeconds());

        return Base64Url.EncodeToString([.. payload, .. HMACSHA256.HashData(_key, payload)]);
    }

    public Guid? Read(string token)
    {
        byte[] bytes;
        try
        {
            bytes = Base64Url.DecodeFromChars(token);
        }
        catch (FormatException)
        {
            return null;
        }

        if (bytes.Length != PayloadLength + MacLength)
        {
            return null;
        }

        var payload = bytes.AsSpan(0, PayloadLength);
        if (!CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(_key, payload), bytes.AsSpan(PayloadLength)))
        {
            return null;
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(BinaryPrimitives.ReadInt64BigEndian(payload[16..]));
        return expiresAt < timeProvider.GetUtcNow() ? null : new Guid(payload[..16]);
    }
}
