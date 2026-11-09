using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TaskBoard.Application.Abstractions.Authentication;

namespace TaskBoard.Infrastructure.Authentication;

internal sealed class PasswordResetTokenProvider(IOptions<PasswordResetOptions> options, TimeProvider timeProvider)
    : IPasswordResetTokenProvider
{
    private const int TokenBytes = 32;

    public GeneratedPasswordResetToken Create()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
        var lifetime = options.Value.TokenLifetime;

        return new GeneratedPasswordResetToken(token, Hash(token), timeProvider.GetUtcNow().Add(lifetime), lifetime);
    }

    // A plain hash is enough: the token is 256 random bits, so there is nothing to brute-force or look up.
    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
