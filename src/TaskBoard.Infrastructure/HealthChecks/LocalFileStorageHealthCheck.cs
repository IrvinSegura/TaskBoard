using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TaskBoard.Infrastructure.Storage;

namespace TaskBoard.Infrastructure.HealthChecks;

internal sealed class LocalFileStorageHealthCheck(IOptions<FileStorageOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Fails when the folder cannot be created, e.g. a read-only or unmounted volume.
        Directory.CreateDirectory(Path.GetFullPath(options.Value.LocalRootPath));
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
