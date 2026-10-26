using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Attachments;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class AttachmentRepository(ApplicationDbContext db) : IAttachmentRepository
{
    public async Task<Attachment?> GetByIdAsync(Guid attachmentId, CancellationToken cancellationToken) =>
        await db.Attachments.FindAsync([attachmentId], cancellationToken);

    public void Add(Attachment attachment) => db.Attachments.Add(attachment);

    public void Remove(Attachment attachment) => db.Attachments.Remove(attachment);
}
