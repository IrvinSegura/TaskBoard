namespace TaskBoard.Application.Features.Attachments.UploadAttachment;

/// <summary>
/// Checks the first bytes of a file against the known signature for its extension,
/// so a renamed executable cannot pass as an image or PDF.
/// </summary>
internal static class FileSignatures
{
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".gif"] = [[0x47, 0x49, 0x46, 0x38]],
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46]],
        [".zip"] = [ZipSignature],
        [".docx"] = [ZipSignature],
        [".xlsx"] = [ZipSignature]
    };

    public static async Task<bool> MatchesAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        // Plain-text formats (.txt, .csv) have no signature to check.
        if (!Signatures.TryGetValue(extension, out var candidates))
        {
            return true;
        }

        if (!content.CanSeek)
        {
            return false;
        }

        var header = new byte[candidates.Max(s => s.Length)];
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;

        return candidates.Any(signature => read >= signature.Length && header.AsSpan(0, signature.Length).SequenceEqual(signature));
    }
}
