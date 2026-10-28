using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.RenameConversation;

internal sealed class RenameConversationCommandHandler(
    IConversationRepository conversations,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RenameConversationCommand>
{
    public async Task<Result> Handle(RenameConversationCommand command, CancellationToken cancellationToken)
    {
        var conversation = await conversations.GetByIdAsync(command.ConversationId, cancellationToken);
        if (conversation is null)
        {
            return ChatErrors.NotFound(command.ConversationId);
        }

        var result = conversation.Rename(currentUser.UserId, command.Title);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
