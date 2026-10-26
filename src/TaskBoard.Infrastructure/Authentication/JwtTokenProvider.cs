using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Authentication;

internal sealed class JwtTokenProvider(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenProvider
{
    private const int RefreshTokenBytes = 32;

    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessToken CreateAccessToken(User user)
    {
        var jwt = options.Value;
        var expiresAt = timeProvider.GetUtcNow().Add(jwt.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.DisplayName)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expiresAt);
    }

    public GeneratedRefreshToken CreateRefreshToken()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        var expiresAt = timeProvider.GetUtcNow().Add(options.Value.RefreshTokenLifetime);

        return new GeneratedRefreshToken(token, HashRefreshToken(token), expiresAt);
    }

    // A plain hash is enough: the token is 256 random bits, so there is nothing to brute-force or look up.
    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
