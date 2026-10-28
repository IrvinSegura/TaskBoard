using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.AddConversationParticipants;

internal sealed class AddConversationParticipantsCommandHandler(
    IReadDbContext db,
    IConversationRepository conversations,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddConversationParticipantsCommand>
{
    public async Task<Result> Handle(AddConversationParticipantsCommand command, CancellationToken cancellationToken)
    {
        var conversation = await conversations.GetByIdAsync(command.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return ChatErrors.NotFound(command.ConversationId);
        }

        var missing = await ChatUsers.FindMissingAsync(db, command.UserIds, cancellationToken);
        if (missing is not null)
        {
            return ChatErrors.UserNotFound(missing.Value.ToString());
        }

        var result = conversation.AddParticipants(currentUser.UserId, command.UserIds);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
