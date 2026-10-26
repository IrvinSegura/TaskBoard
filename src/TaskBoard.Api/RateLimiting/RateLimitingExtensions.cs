using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace TaskBoard.Api.RateLimiting;

internal static class RateLimitingExtensions
{
    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthRateLimitOptions>().Bind(configuration.GetSection(AuthRateLimitOptions.SectionName));

        services.AddRateLimiter(_ => { });

        // Configured from IOptions so tests can change the limits.
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<AuthRateLimitOptions>>((options, authOptions) =>
            {
                var auth = authOptions.Value;

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = WriteRejectionAsync;

                // Partitioned by client IP. Behind a reverse proxy, list it under ForwardedHeaders
                // or every user will share the proxy's limit.
                options.AddPolicy(AuthRateLimitOptions.PolicyName, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = auth.PermitLimit,
                            Window = auth.Window,
                            QueueLimit = 0
                        }));
            });

        return services;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "RateLimit.Exceeded",
                Detail = "Too many attempts. Try again later."
            }
        });
    }
}
