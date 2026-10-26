using Microsoft.IdentityModel.JsonWebTokens;
using TaskBoard.Application.Abstractions.Authentication;

namespace TaskBoard.Api.Services;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public Guid UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("There is no authenticated user in the current request.");
}
