namespace TaskBoard.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    // "Local" (development) or "AzureBlob".
    public string Provider { get; init; } = "Local";

    // Must be outside wwwroot: files are only served through the authorized download endpoint.
    public string LocalRootPath { get; init; } = "storage";

    public string? AzureConnectionString { get; init; }

    public string AzureContainerName { get; init; } = "attachments";

    // How often stored files without an attachment row are deleted. Zero turns the cleanup off.
    public TimeSpan OrphanCleanupInterval { get; init; } = TimeSpan.FromHours(6);

    // Younger files are left alone: their upload may still be saving its attachment row.
    public TimeSpan OrphanGracePeriod { get; init; } = TimeSpan.FromHours(1);
}
