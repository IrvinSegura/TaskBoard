using TaskBoard.Domain.Chat.Events;
using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Chat;

// Own aggregate (not inside Conversation) so posting a message never loads the whole history.
public sealed class ChatMessage : AggregateRoot
{
    private readonly List<ChatAttachment> _attachments = [];

    private ChatMessage()
    {
    }

    public Guid ConversationId { get; private set; }

    public Guid SenderId { get; private set; }

    // Null for a message that only carries files, and once the message is deleted.
    public string? Body { get; private set; }

    public bool IsEdited { get; private set; }

    // Deleted messages stay in place as "message deleted", without their text and files.
    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<ChatAttachment> Attachments => _attachments.AsReadOnly();

    public static Result<ChatMessage> Send(
        Guid conversationId,
        Guid senderId,
        string? body,
        IReadOnlyCollection<ChatFile> files)
    {
        var text = Normalize(body);
        if (text is null && files.Count == 0)
        {
            return ChatErrors.EmptyMessage;
        }

        if (files.Count > ChatRules.MaxAttachmentsPerMessage)
        {
            return ChatErrors.TooManyAttachments;
        }

        if (files.Sum(f => f.SizeInBytes) > ChatRules.MaxMessageSizeInBytes)
        {
            return ChatErrors.MessageTooLarge;
        }

        var message = new ChatMessage { ConversationId = conversationId, SenderId = senderId, Body = text };
        message._attachments.AddRange(files.Select((file, position) => new ChatAttachment(message.Id, position, file)));

        message.Raise(new ChatMessageSentEvent(message.Id, conversationId, senderId));
        return message;
    }

    // Changes the text only; the files stay. Clearing the text is fine while the message still has files.
    public Result Edit(Guid actorId, string? body)
    {
        var check = EnsureSenderCanChange(actorId);
        if (check.IsFailure)
        {
            return check;
        }

        var text = Normalize(body);
        if (text is null && _attachments.Count == 0)
        {
            return ChatErrors.EmptyMessage;
        }

        if (text == Body)
        {
            return Result.Success();
        }

        Body = text;
        IsEdited = true;

        Raise(new ChatMessageEditedEvent(Id, ConversationId, Body));
        return Result.Success();
    }

    public Result Delete(Guid actorId)
    {
        var check = EnsureSenderCanChange(actorId);
        if (check.IsFailure)
        {
            return check;
        }

        var storageKeys = _attachments.Select(a => a.StorageKey).ToList();

        Body = null;
        IsDeleted = true;
        _attachments.Clear();

        Raise(new ChatMessageDeletedEvent(Id, ConversationId, storageKeys));
        return Result.Success();
    }

    private Result EnsureSenderCanChange(Guid actorId)
    {
        if (actorId != SenderId)
        {
            return ChatErrors.NotSender;
        }

        return IsDeleted ? ChatErrors.MessageDeleted : Result.Success();
    }

    private static string? Normalize(string? body) => string.IsNullOrWhiteSpace(body) ? null : body.Trim();
}
