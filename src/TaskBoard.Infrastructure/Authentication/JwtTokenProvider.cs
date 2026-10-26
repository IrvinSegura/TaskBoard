using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Features.Auth.Login;
using TaskBoard.Domain.Users;

namespace TaskBoard.Infrastructure.Authentication;

internal sealed class JwtTokenProvider(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenProvider
{
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public AccessTokenResponse CreateAccessToken(User user)
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

        return new AccessTokenResponse(_tokenHandler.CreateToken(descriptor), expiresAt);
    }
}
