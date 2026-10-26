using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Storage;
using TaskBoard.Domain.Attachments;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Attachments.DownloadAttachment;

internal sealed class DownloadAttachmentQueryHandler(IReadDbContext db, IFileStorage fileStorage)
    : IQueryHandler<DownloadAttachmentQuery, AttachmentFileResponse>
{
    public async Task<Result<AttachmentFileResponse>> Handle(
        DownloadAttachmentQuery query,
        CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments
            .Where(a => a.Id == query.AttachmentId)
            .Select(a => new { a.StorageKey, a.ContentType, a.FileName })
            .SingleOrDefaultAsync(cancellationToken);
        if (attachment is null)
        {
            return AttachmentErrors.NotFound(query.AttachmentId);
        }

        var content = await fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
        if (content is null)
        {
            return AttachmentErrors.FileMissing;
        }

        return new AttachmentFileResponse(content, attachment.ContentType, attachment.FileName);
    }
}
