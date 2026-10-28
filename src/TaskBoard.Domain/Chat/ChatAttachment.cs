using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Chat;

public sealed class ChatAttachment : Entity
{
    private ChatAttachment()
    {
    }

    internal ChatAttachment(Guid messageId, int position, ChatFile file)
    {
        MessageId = messageId;
        Position = position;
        FileName = file.FileName;
        ContentType = file.ContentType;
        SizeInBytes = file.SizeInBytes;
        StorageKey = file.StorageKey;
    }

    public Guid MessageId { get; private set; }

    // Upload order. Ids made in the same millisecond are not ordered, so they cannot keep it.
    public int Position { get; private set; }

    // Original name, only for display and download. The file itself is stored under StorageKey.
    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long SizeInBytes { get; private set; }

    public string StorageKey { get; private set; } = null!;
}

// A file already in storage, about to be attached to a message.
public sealed record ChatFile(string FileName, string ContentType, long SizeInBytes, string StorageKey);
