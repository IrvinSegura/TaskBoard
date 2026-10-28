using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.MarkConversationRead;

internal sealed class MarkConversationReadCommandHandler(
    ChatAccess access,
    IConversationRepository conversations,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IRealtimeNotifier notifier,
    TimeProvider timeProvider)
    : ICommandHandler<MarkConversationReadCommand>
{
    public async Task<Result> Handle(MarkConversationReadCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (!await access.CanOpenAsync(command.ConversationId, userId, cancellationToken))
        {
            return ChatErrors.NotFound(command.ConversationId);
        }

        var now = timeProvider.GetUtcNow();
        var marker = await conversations.GetReadMarkerAsync(command.ConversationId, userId, cancellationToken);
        if (marker is null)
        {
            conversations.AddReadMarker(ConversationReadMarker.Create(command.ConversationId, userId, now));
        }
        else
        {
            marker.MarkRead(now);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (marker is null)
        {
            // Another tab created the marker at the same moment; it already marks the conversation as read.
        }

        await notifier.User(userId).ConversationRead(new ConversationReadMessage(command.ConversationId));
        return Result.Success();
    }
}
