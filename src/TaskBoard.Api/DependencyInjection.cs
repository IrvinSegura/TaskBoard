using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskBoard.Api.Extensions;
using TaskBoard.Api.Middleware;
using TaskBoard.Api.Realtime;
using TaskBoard.Api.Services;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Attachments;
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

        services.AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSingleton<IUserIdProvider, SubjectUserIdProvider>();
        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

        // Leave room above the file limit for the multipart envelope.
        services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = AttachmentRules.MaxSizeInBytes + 64 * 1024);

        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
            .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        services.AddJwtAuthentication();
        services.AddAuthorization();

        services.AddEndpoints(typeof(DependencyInjection).Assembly);

        return services;
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
