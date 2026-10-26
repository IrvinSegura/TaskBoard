using TaskBoard.Domain.Attachments.Events;
using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Attachments;

public sealed class Attachment : AggregateRoot
{
    private Attachment()
    {
    }

    public Guid CardId { get; private set; }

    public Guid BoardId { get; private set; }

    public Guid UploadedById { get; private set; }

    // Original name, only for display and download. The file itself is stored under StorageKey.
    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long SizeInBytes { get; private set; }

    public string StorageKey { get; private set; } = null!;

    public static Attachment Create(
        Guid cardId,
        Guid boardId,
        Guid uploadedById,
        string fileName,
        string contentType,
        long sizeInBytes,
        string storageKey)
    {
        var attachment = new Attachment
        {
            CardId = cardId,
            BoardId = boardId,
            UploadedById = uploadedById,
            FileName = fileName,
            ContentType = contentType,
            SizeInBytes = sizeInBytes,
            StorageKey = storageKey
        };

        attachment.Raise(new AttachmentAddedEvent(attachment.Id, cardId, boardId, fileName));
        return attachment;
    }
}
