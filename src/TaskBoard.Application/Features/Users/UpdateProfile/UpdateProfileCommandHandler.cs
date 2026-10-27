using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Users.UpdateProfile;

internal sealed class UpdateProfileCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProfileCommand>
{
    public async Task<Result> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken);

        // An access token can outlive the account it was issued for.
        if (user is null)
        {
            return UserErrors.NotFound(currentUser.UserId);
        }

        user.Rename(command.DisplayName);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
