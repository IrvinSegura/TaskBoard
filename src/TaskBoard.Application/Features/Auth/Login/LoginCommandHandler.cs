using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Auth.Login;

internal sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider)
    : ICommandHandler<LoginCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(command.Email, cancellationToken);

        // Same error for unknown email and wrong password, so accounts cannot be enumerated.
        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return UserErrors.InvalidCredentials;
        }

        return tokenProvider.CreateAccessToken(user);
    }
}
