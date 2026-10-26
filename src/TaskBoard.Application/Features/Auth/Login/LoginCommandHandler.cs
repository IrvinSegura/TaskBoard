using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Auth.Login;

internal sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    AuthTokenService tokenService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LoginCommand, AuthTokensResponse>
{
    public async Task<Result<AuthTokensResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(command.Email, cancellationToken);

        // Same error for unknown email and wrong password, so accounts cannot be enumerated.
        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return UserErrors.InvalidCredentials;
        }

        var tokens = tokenService.Issue(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return tokens;
    }
}
