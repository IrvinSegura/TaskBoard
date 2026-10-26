using System.Runtime.CompilerServices;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using TaskBoard.Application.Abstractions.Storage;

namespace TaskBoard.Infrastructure.Storage;

internal sealed class AzureBlobFileStorage(BlobServiceClient blobServiceClient, IOptions<FileStorageOptions> options)
    : IFileStorage
{
    private BlobContainerClient Container => blobServiceClient.GetBlobContainerClient(options.Value.AzureContainerName);

    public async Task<string> UploadAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        // TODO: create the container once at startup instead of on every upload.
        await Container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var storageKey = Guid.NewGuid().ToString("N");
        var uploadOptions = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };
        await Container.GetBlobClient(storageKey).UploadAsync(content, uploadOptions, cancellationToken);

        return storageKey;
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            var download = await Container.GetBlobClient(storageKey)
                .DownloadStreamingAsync(cancellationToken: cancellationToken);
            return download.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken) =>
        Container.GetBlobClient(storageKey).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    public async IAsyncEnumerable<StoredFile> ListAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var container = Container;

        // Created on the first upload, so a missing container just means there are no files yet.
        if (!await container.ExistsAsync(cancellationToken))
        {
            yield break;
        }

        await foreach (var blob in container.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            // Without a timestamp the file's age is unknown, so it is never treated as old enough to delete.
            yield return new StoredFile(blob.Name, blob.Properties.LastModified ?? DateTimeOffset.MaxValue);
        }
    }
}
