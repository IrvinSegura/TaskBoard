using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.DeleteChatMessage;

internal sealed class DeleteChatMessageCommandHandler(
    IChatMessageRepository messages,
    ChatAccess access,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteChatMessageCommand>
{
    public async Task<Result> Handle(DeleteChatMessageCommand command, CancellationToken cancellationToken)
    {
        var message = await messages.GetByIdAsync(command.MessageId, cancellationToken);
        if (message is null || !await access.CanOpenAsync(message.ConversationId, currentUser.UserId, cancellationToken))
        {
            return ChatErrors.MessageNotFound(command.MessageId);
        }

        var result = message.Delete(currentUser.UserId);
        if (result.IsFailure)
        {
            return result;
        }

        // The stored files are deleted by ChatMessageDeletedEventHandler, only after their rows are gone.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
