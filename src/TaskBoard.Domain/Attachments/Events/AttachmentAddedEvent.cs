using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Attachments.Events;

public sealed record AttachmentAddedEvent(Guid AttachmentId, Guid CardId, Guid BoardId, string FileName)
    : IDomainEvent;
