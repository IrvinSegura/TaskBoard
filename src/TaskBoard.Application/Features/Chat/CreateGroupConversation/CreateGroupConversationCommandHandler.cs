using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.CreateGroupConversation;

internal sealed class CreateGroupConversationCommandHandler(
    IReadDbContext db,
    IConversationRepository conversations,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateGroupConversationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateGroupConversationCommand command, CancellationToken cancellationToken)
    {
        var missing = await ChatUsers.FindMissingAsync(db, command.ParticipantIds, cancellationToken);
        if (missing is not null)
        {
            return ChatErrors.UserNotFound(missing.Value.ToString());
        }

        var started = Conversation.StartGroup(currentUser.UserId, command.Title, command.ParticipantIds);
        if (started.IsFailure)
        {
            return started.Error;
        }

        conversations.Add(started.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return started.Value.Id;
    }
}
