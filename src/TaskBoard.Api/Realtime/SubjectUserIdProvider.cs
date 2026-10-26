using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace TaskBoard.Api.Realtime;

// Inbound claim mapping is off, so the user id is in "sub" rather than ClaimTypes.NameIdentifier.
internal sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
