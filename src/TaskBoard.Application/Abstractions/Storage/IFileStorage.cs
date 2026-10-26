namespace TaskBoard.Application.Abstractions.Storage;

public interface IFileStorage
{
    // Returns the generated storage key; the original file name is never used on disk.
    Task<string> UploadAsync(Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
