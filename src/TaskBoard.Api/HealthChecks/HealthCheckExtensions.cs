using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskBoard.Api.HealthChecks;

internal static class HealthCheckExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// /health/live answers while the process runs (liveness); /health also checks PostgreSQL,
    /// file storage and Redis (readiness). Both are anonymous, so they report statuses but no error details.
    /// </summary>
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteReportAsync
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(Infrastructure.DependencyInjection.ReadinessTag),
            ResponseWriter = WriteReportAsync
        });

        return app;
    }

    // Failure details stay in the logs, where the health check service writes them.
    private static Task WriteReportAsync(HttpContext context, HealthReport report)
    {
        var response = new
        {
            Status = report.Status.ToString(),
            DurationMs = Math.Round(report.TotalDuration.TotalMilliseconds),
            Checks = report.Entries.Select(entry => new
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                DurationMs = Math.Round(entry.Value.Duration.TotalMilliseconds)
            })
        };

        return context.Response.WriteAsJsonAsync(response, JsonOptions);
    }
}
