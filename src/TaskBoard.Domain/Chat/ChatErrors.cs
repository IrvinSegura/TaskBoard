using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Chat;

public static class ChatErrors
{
    public static readonly Error CannotMessageYourself =
        Error.Validation("Chat.CannotMessageYourself", "A direct conversation needs another person.");

    public static readonly Error NotAGroup =
        Error.Validation("Chat.NotAGroup", "Only group conversations can be renamed or change participants.");

    public static readonly Error GroupNeedsParticipants =
        Error.Validation("Chat.GroupNeedsParticipants", "A group needs at least one other person.");

    public static readonly Error TooManyParticipants =
        Error.Validation("Chat.TooManyParticipants", $"A group can have at most {ChatRules.MaxGroupParticipants} people.");

    public static readonly Error EmptyMessage =
        Error.Validation("Chat.EmptyMessage", "A message needs text or at least one file.");

    public static readonly Error TooManyAttachments =
        Error.Validation("Chat.TooManyAttachments", $"A message can have at most {ChatRules.MaxAttachmentsPerMessage} files.");

    public static readonly Error MessageTooLarge =
        Error.Validation("Chat.MessageTooLarge", "The files of this message are too large together.");

    public static readonly Error TypeNotAllowed =
        Error.Validation("Chat.TypeNotAllowed", "This file type is not allowed.");

    public static readonly Error ContentMismatch =
        Error.Validation("Chat.ContentMismatch", "The file content does not match its extension.");

    public static readonly Error NotSender =
        Error.Forbidden("Chat.NotSender", "Only the sender can change this message.");

    public static readonly Error MessageDeleted =
        Error.Conflict("Chat.MessageDeleted", "This message was deleted.");

    public static readonly Error FileMissing =
        Error.NotFound("Chat.FileMissing", "The stored file could not be found.");

    public static Error NotFound(Guid conversationId) =>
        Error.NotFound("Chat.NotFound", $"Conversation '{conversationId}' was not found.");

    public static Error MessageNotFound(Guid messageId) =>
        Error.NotFound("Chat.MessageNotFound", $"Message '{messageId}' was not found.");

    public static Error AttachmentNotFound(Guid attachmentId) =>
        Error.NotFound("Chat.AttachmentNotFound", $"File '{attachmentId}' was not found.");

    public static Error UserNotFound(string user) =>
        Error.NotFound("Chat.UserNotFound", $"User '{user}' was not found.");
}
