using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TaskBoard.Api.Extensions;
using TaskBoard.Api.Middleware;
using TaskBoard.Api.RateLimiting;
using TaskBoard.Api.Realtime;
using TaskBoard.Api.Services;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Attachments;
using TaskBoard.Infrastructure;
using TaskBoard.Infrastructure.Authentication;

namespace TaskBoard.Api;

public static class DependencyInjection
{
    public const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddRealtime(configuration);

        // Leave room above the file limit for the multipart envelope.
        services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = AttachmentRules.MaxSizeInBytes + 64 * 1024);

        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
            .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        services.AddJwtAuthentication();
        services.AddAuthorization();
        services.AddTrustedForwardedHeaders(configuration);
        services.AddAuthRateLimiting(configuration);

        services.AddEndpoints(typeof(DependencyInjection).Assembly);

        return services;
    }

    private static void AddRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        var signalR = services.AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
        services.AddSingleton<UserConnectionTracker>();
        services.AddSingleton<LocalBoardGroupEvictor>();
        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

        var redisConnectionString = configuration.GetRedisConnectionString();
        if (redisConnectionString is null)
        {
            services.AddSingleton<IBoardGroupEvictor>(sp => sp.GetRequiredService<LocalBoardGroupEvictor>());
            return;
        }

        // Backplane: a message sent on one instance reaches clients connected to any instance.
        signalR.AddStackExchangeRedis(redisConnectionString, options =>
            options.Configuration.ChannelPrefix = RedisChannel.Literal("taskboard"));

        services.AddSingleton<IBoardGroupEvictor, RedisBoardGroupEvictor>();
        services.AddHostedService<RedisBoardGroupEvictionListener>();
    }

    private static void AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured from IOptions<JwtOptions> so tests and user-secrets can override the values.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Keep "sub", "email", "name" as issued instead of mapping them to long SOAP claim URIs.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                // Browsers cannot set headers on WebSocket requests, so SignalR sends the token in the query string.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
    }
}
