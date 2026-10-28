namespace TaskBoard.Application.Common.Files;

/// <summary>
/// Checks the first bytes of a file against the known signature for its extension,
/// so a renamed executable cannot pass as an image, a PDF or a recording.
/// </summary>
internal static class FileSignatures
{
    // Null matches any byte, for the size fields that sit before a format's marker.
    private static readonly byte?[] Zip = [0x50, 0x4B, 0x03, 0x04];

    // EBML header shared by WebM video and audio.
    private static readonly byte?[] Ebml = [0x1A, 0x45, 0xDF, 0xA3];

    // ISO base media ("....ftyp"): MP4, M4A and QuickTime.
    private static readonly byte?[] IsoMedia = [null, null, null, null, 0x66, 0x74, 0x79, 0x70];

    private static readonly Dictionary<string, byte?[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".gif"] = [[0x47, 0x49, 0x46, 0x38]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46, null, null, null, null, 0x57, 0x45, 0x42, 0x50]],
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46]],
        [".zip"] = [Zip],
        [".docx"] = [Zip],
        [".xlsx"] = [Zip],
        [".pptx"] = [Zip],
        // An ID3 tag, or straight away an MPEG audio frame.
        [".mp3"] = [[0x49, 0x44, 0x33], [0xFF, 0xFB], [0xFF, 0xF3], [0xFF, 0xF2]],
        [".wav"] = [[0x52, 0x49, 0x46, 0x46, null, null, null, null, 0x57, 0x41, 0x56, 0x45]],
        [".ogg"] = [[0x4F, 0x67, 0x67, 0x53]],
        [".weba"] = [Ebml],
        [".webm"] = [Ebml],
        [".m4a"] = [IsoMedia],
        [".mp4"] = [IsoMedia],
        [".mov"] = [IsoMedia]
    };

    public static async Task<bool> MatchesAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        // Plain-text formats (.txt, .csv, .md) have no signature to check.
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

        return candidates.Any(signature => read >= signature.Length && Matches(header, signature));
    }

    private static bool Matches(byte[] header, byte?[] signature)
    {
        for (var i = 0; i < signature.Length; i++)
        {
            if (signature[i] is { } expected && header[i] != expected)
            {
                return false;
            }
        }

        return true;
    }
}
