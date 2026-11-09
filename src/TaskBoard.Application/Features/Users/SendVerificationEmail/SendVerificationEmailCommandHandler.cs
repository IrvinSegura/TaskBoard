using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Users.SendVerificationEmail;

// A confirmed email has nothing left to confirm, so asking again sends nothing.
internal sealed class SendVerificationEmailCommandHandler(
    ICurrentUser currentUser,
    IUserRepository users,
    IEmailVerificationTokens tokens,
    AccountEmails emails,
    IEmailSender sender)
    : ICommandHandler<SendVerificationEmailCommand>
{
    public async Task<Result> Handle(SendVerificationEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound(currentUser.UserId);
        }

        if (!user.IsEmailVerified)
        {
            await sender.SendAsync(
                emails.VerifyEmail(user.Email, user.DisplayName, tokens.Create(user.Id), tokens.ValidFor),
                cancellationToken);
        }

        return Result.Success();
    }
}
