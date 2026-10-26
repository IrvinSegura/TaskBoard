using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace TaskBoard.Api.Extensions;

/// <summary>
/// Behind a reverse proxy every request comes from the proxy's address. Listing the trusted proxies lets
/// the API read the client's address from X-Forwarded-For, which the per-IP rate limit depends on.
/// </summary>
internal static class ForwardedHeadersExtensions
{
    private const string SectionName = "ForwardedHeaders";

    public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var knownProxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];
        var knownNetworks = section.GetSection("KnownNetworks").Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Only the configured proxies are trusted. An empty list trusts nobody (beyond loopback),
            // never everybody, so a client cannot spoof its address to dodge the rate limit.
            foreach (var proxy in knownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in knownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return services;
    }
}
