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
}
