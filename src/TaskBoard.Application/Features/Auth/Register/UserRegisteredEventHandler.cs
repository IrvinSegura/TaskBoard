using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Domain.Users.Events;

namespace TaskBoard.Application.Features.Auth.Register;

/// <summary>
/// Welcomes a new user, with the link that confirms their email. From the outbox, so signing up never waits
/// for the mail server.
/// </summary>
internal sealed class UserRegisteredEventHandler(
    IUserRepository users,
    IEmailVerificationTokens tokens,
    AccountEmails emails,
    IEmailSender sender)
    : IDomainEventHandler<UserRegisteredEvent>
{
    public async Task Handle(UserRegisteredEvent domainEvent, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(domainEvent.UserId, cancellationToken);
        if (user is null || user.IsEmailVerified)
        {
            return;
        }

        await sender.SendAsync(
            emails.Welcome(user.Email, user.DisplayName, tokens.Create(user.Id), tokens.ValidFor),
            cancellationToken);
    }
}
