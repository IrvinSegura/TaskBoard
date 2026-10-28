using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Storage;
using TaskBoard.Application.Features.Attachments.DownloadAttachment;
using TaskBoard.Domain.Chat;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Chat.DownloadChatAttachment;

internal sealed class DownloadChatAttachmentQueryHandler(
    IReadDbContext db,
    ChatAccess access,
    IFileStorage fileStorage,
    ICurrentUser currentUser)
    : IQueryHandler<DownloadChatAttachmentQuery, AttachmentFileResponse>
{
    public async Task<Result<AttachmentFileResponse>> Handle(
        DownloadChatAttachmentQuery query,
        CancellationToken cancellationToken)
    {
        var attachment = await (
            from file in db.ChatAttachments
            join message in db.ChatMessages on file.MessageId equals message.Id
            where file.Id == query.AttachmentId
            select new { message.ConversationId, file.StorageKey, file.ContentType, file.FileName })
            .SingleOrDefaultAsync(cancellationToken);
        if (attachment is null ||
            !await access.CanOpenAsync(attachment.ConversationId, currentUser.UserId, cancellationToken))
        {
            return ChatErrors.AttachmentNotFound(query.AttachmentId);
        }

        var content = await fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        if (content is null)
        {
            return ChatErrors.FileMissing;
        }

        return new AttachmentFileResponse(content, attachment.ContentType, attachment.FileName);
    }
}
