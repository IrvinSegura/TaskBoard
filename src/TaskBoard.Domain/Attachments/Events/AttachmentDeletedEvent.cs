using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Attachments.Events;

// Carries the storage key so the stored file can be deleted after the row is gone.
public sealed record AttachmentDeletedEvent(Guid AttachmentId, Guid CardId, Guid BoardId, string StorageKey)
    : IDomainEvent;
