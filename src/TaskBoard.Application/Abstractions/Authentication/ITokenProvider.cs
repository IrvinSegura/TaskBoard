using TaskBoard.Application.Features.Auth.Login;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Abstractions.Authentication;

public interface ITokenProvider
{
    AccessTokenResponse CreateAccessToken(User user);
}
