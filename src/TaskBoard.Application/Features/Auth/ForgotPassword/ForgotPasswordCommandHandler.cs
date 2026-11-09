using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Auth.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IPasswordResetTokenRepository resetTokens,
    IPasswordResetTokenProvider tokenProvider,
    AccountEmails emails,
    IEmailSender sender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        // Succeeds whether the account exists or not, so this cannot be used to find out who has one.
        var user = await users.GetByEmailAsync(command.Email, cancellationToken);
        if (user is null)
        {
            return Result.Success();
        }

        var now = timeProvider.GetUtcNow();
        var active = await resetTokens.GetActiveForUserAsync(user.Id, now, cancellationToken);
        if (active.Any(t => t.CreatedAt > now - PasswordResetToken.RequestCooldown))
        {
            return Result.Success();
        }

        // Only the latest link works.
        foreach (var token in active)
        {
            token.Consume(now);
        }

        var generated = tokenProvider.Create();
        resetTokens.Add(PasswordResetToken.Create(user.Id, generated.Hash, generated.ExpiresAt));

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A parallel request replaced the same tokens first, and it sends the email.
            return Result.Success();
        }

        await sender.SendAsync(
            emails.ResetPassword(user.Email, user.DisplayName, generated.Token, generated.ValidFor),
            cancellationToken);

        return Result.Success();
    }
}
