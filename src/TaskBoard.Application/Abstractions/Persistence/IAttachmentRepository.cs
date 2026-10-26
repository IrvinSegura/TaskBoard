using TaskBoard.Domain.Attachments;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IAttachmentRepository
{
    Task<Attachment?> GetByIdAsync(Guid attachmentId, CancellationToken cancellationToken);

    void Add(Attachment attachment);

    void Remove(Attachment attachment);
}
