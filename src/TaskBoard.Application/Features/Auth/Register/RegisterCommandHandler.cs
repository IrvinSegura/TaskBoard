using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Auth.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        if (await users.ExistsByEmailAsync(command.Email, cancellationToken))
        {
            return UserErrors.EmailTaken;
        }

        var user = User.Create(command.Email, command.DisplayName, passwordHasher.Hash(command.Password));
        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
