using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.EditChatMessage;

internal sealed class EditChatMessageCommandHandler(
    IChatMessageRepository messages,
    ChatAccess access,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<EditChatMessageCommand>
{
    public async Task<Result> Handle(EditChatMessageCommand command, CancellationToken cancellationToken)
    {
        // Someone who lost access to the conversation cannot change what they wrote there either.
        var message = await messages.GetByIdAsync(command.MessageId, cancellationToken);
        if (message is null || !await access.CanOpenAsync(message.ConversationId, currentUser.UserId, cancellationToken))
        {
            return ChatErrors.MessageNotFound(command.MessageId);
        }

        var result = message.Edit(currentUser.UserId, command.Body);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
