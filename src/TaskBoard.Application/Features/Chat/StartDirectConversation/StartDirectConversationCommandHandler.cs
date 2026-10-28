using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Chat.StartDirectConversation;

internal sealed class StartDirectConversationCommandHandler(
    IReadDbContext db,
    IConversationRepository conversations,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<StartDirectConversationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(StartDirectConversationCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email is null ? null : User.NormalizeEmail(command.Email);
        var otherUserId = await db.Users
            .Where(u => command.UserId.HasValue ? u.Id == command.UserId : u.Email == email)
            .Select(u => (Guid?)u.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (otherUserId is null)
        {
            return ChatErrors.UserNotFound(command.UserId?.ToString() ?? command.Email!);
        }

        var userId = currentUser.UserId;
        var existing = await conversations.GetDirectAsync(userId, otherUserId.Value, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var started = Conversation.StartDirect(userId, otherUserId.Value);
        if (started.IsFailure)
        {
            return started.Error;
        }

        conversations.Add(started.Value);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Both people opened the conversation at the same moment; the unique key kept the other one.
            var winner = await conversations.GetDirectAsync(userId, otherUserId.Value, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            return winner.Id;
        }

        return started.Value.Id;
    }
}
