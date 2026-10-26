using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Auth.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    AuthTokenService tokenService,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(currentUser.UserId);
        }

        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            return UserErrors.WrongPassword;
        }

        user.ChangePassword(passwordHasher.Hash(command.NewPassword));

        // Anyone who knew the old password may hold a session; end them all.
        await tokenService.RevokeAllAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
