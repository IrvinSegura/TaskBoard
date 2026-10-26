using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TaskBoard.Infrastructure.Storage;

namespace TaskBoard.Infrastructure.HealthChecks;

// Healthy when the account answers; the container may not exist yet because it is created on the first upload.
internal sealed class AzureBlobStorageHealthCheck(BlobServiceClient blobServiceClient, IOptions<FileStorageOptions> options)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await blobServiceClient.GetBlobContainerClient(options.Value.AzureContainerName).ExistsAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
