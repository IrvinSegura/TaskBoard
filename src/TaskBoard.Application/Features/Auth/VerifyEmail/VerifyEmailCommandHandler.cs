using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Auth.VerifyEmail;

// Anonymous, so the link works in any browser: the signed token is what proves the address.
internal sealed class VerifyEmailCommandHandler(
    IEmailVerificationTokens tokens,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<VerifyEmailCommand>
{
    public async Task<Result> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        if (tokens.Read(command.Token) is not { } userId
            || await users.GetByIdAsync(userId, cancellationToken) is not { } user)
        {
            return UserErrors.InvalidEmailVerificationToken;
        }

        user.VerifyEmail(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
