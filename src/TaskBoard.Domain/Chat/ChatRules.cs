using System.Diagnostics.CodeAnalysis;

namespace TaskBoard.Domain.Chat;

public static class ChatRules
{
    public const int MaxBodyLength = 4000;

    public const int MaxTitleLength = 100;

    // The creator included.
    public const int MaxGroupParticipants = 50;

    public const int MaxAttachmentsPerMessage = 10;

    public const int MaxFileNameLength = 255;

    public const long MaxFileSizeInBytes = 25 * 1024 * 1024;

    // All the files of one message together.
    public const long MaxMessageSizeInBytes = 50 * 1024 * 1024;

    // The content type is derived from the extension, never trusted from the client.
    // Voice notes recorded in the browser arrive as .weba (Chrome, Firefox) or .m4a (Safari).
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".ogg"] = "audio/ogg",
        [".weba"] = "audio/webm",
        [".m4a"] = "audio/mp4",
        [".mp4"] = "video/mp4",
        [".webm"] = "video/webm",
        [".mov"] = "video/quicktime",
        [".pdf"] = "application/pdf",
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".csv"] = "text/csv",
        [".zip"] = "application/zip",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation"
    };

    public static bool IsAllowedExtension(string extension) => ContentTypes.ContainsKey(extension);

    public static bool TryGetContentType(string extension, [NotNullWhen(true)] out string? contentType) =>
        ContentTypes.TryGetValue(extension, out contentType);

    // The same key whatever the order, so a unique index keeps one direct conversation per pair.
    public static string DirectKey(Guid userId, Guid otherUserId) =>
        userId.CompareTo(otherUserId) < 0 ? $"{userId:N}:{otherUserId:N}" : $"{otherUserId:N}:{userId:N}";
}
