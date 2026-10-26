using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Attachments;

public static class AttachmentErrors
{
    public static readonly Error TypeNotAllowed =
        Error.Validation("Attachments.TypeNotAllowed", "This file type is not allowed.");

    public static readonly Error ContentMismatch =
        Error.Validation("Attachments.ContentMismatch", "The file content does not match its extension.");

    public static readonly Error FileMissing =
        Error.NotFound("Attachments.FileMissing", "The stored file could not be found.");

    public static readonly Error CannotDelete =
        Error.Forbidden("Attachments.CannotDelete", "Only the uploader or a board admin can delete this attachment.");

    public static Error NotFound(Guid attachmentId) =>
        Error.NotFound("Attachments.NotFound", $"Attachment '{attachmentId}' was not found.");
}
