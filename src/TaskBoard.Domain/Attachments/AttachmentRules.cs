using System.Diagnostics.CodeAnalysis;

namespace TaskBoard.Domain.Attachments;

public static class AttachmentRules
{
    public const long MaxSizeInBytes = 10 * 1024 * 1024;

    public const int MaxFileNameLength = 255;

    // The content type is derived from the extension, never trusted from the client.
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".pdf"] = "application/pdf",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv",
        [".zip"] = "application/zip",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    public static bool IsAllowedExtension(string extension) => ContentTypes.ContainsKey(extension);

    public static bool TryGetContentType(string extension, [NotNullWhen(true)] out string? contentType) =>
        ContentTypes.TryGetValue(extension, out contentType);
}
