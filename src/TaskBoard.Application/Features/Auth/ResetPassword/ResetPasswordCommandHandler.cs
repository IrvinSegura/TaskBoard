using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Auth.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IPasswordResetTokenRepository resetTokens,
    IPasswordResetTokenProvider tokenProvider,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    AuthTokenService tokenService,
    AccountEmails emails,
    IEmailSender sender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // One error for an unknown, expired or spent token: telling them apart helps nobody but an attacker.
        var stored = await resetTokens.GetByHashAsync(tokenProvider.Hash(command.Token), cancellationToken);
        if (stored is null || !stored.IsActive(now))
        {
            return UserErrors.InvalidPasswordResetToken;
        }

        var user = await users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null)
        {
            return UserErrors.InvalidPasswordResetToken;
        }

        user.ChangePassword(passwordHasher.Hash(command.NewPassword));

        foreach (var token in await resetTokens.GetActiveForUserAsync(user.Id, now, cancellationToken))
        {
            token.Consume(now);
        }

        // Whoever made the user lose the password may hold a session; end them all.
        await tokenService.RevokeAllAsync(user.Id, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A parallel request used the same token first; only one of them sets the password.
            return UserErrors.InvalidPasswordResetToken;
        }

        await sender.SendAsync(emails.PasswordChanged(user.Email, user.DisplayName), cancellationToken);

        return Result.Success();
    }
}
