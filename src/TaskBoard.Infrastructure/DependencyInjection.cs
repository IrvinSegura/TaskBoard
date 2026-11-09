using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Email;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Storage;
using TaskBoard.Infrastructure.Authentication;
using TaskBoard.Infrastructure.Authorization;
using TaskBoard.Infrastructure.Email;
using TaskBoard.Infrastructure.HealthChecks;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Persistence.Interceptors;
using TaskBoard.Infrastructure.Persistence.Outbox;
using TaskBoard.Infrastructure.Persistence.Repositories;
using TaskBoard.Infrastructure.Reminders;
using TaskBoard.Infrastructure.Storage;

namespace TaskBoard.Infrastructure;

public static class DependencyInjection
{
    // Tag of the dependency checks that /health runs. Each one has a timeout so a hanging dependency cannot hang the probe.
    public const string ReadinessTag = "ready";

    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        return services
            .AddRedis(configuration)
            .AddPersistence(configuration)
            .AddAuthenticationServices(configuration)
            .AddFileStorage(configuration)
            .AddEmail(configuration);
    }

    // Empty when the API runs as a single instance; Redis is needed as soon as there are several.
    public static string? GetRedisConnectionString(this IConfiguration configuration) =>
        configuration.GetConnectionString("Redis") is { Length: > 0 } connectionString ? connectionString : null;

    private static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetRedisConnectionString();
        if (redisConnectionString is null)
        {
            // One instance: an in-process cache is shared by everything that needs it.
            services.AddDistributedMemoryCache();
            return services;
        }

        // Connects in the background and keeps retrying, so a Redis outage does not stop the API from starting.
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(redisOptions);
        });

        services.AddStackExchangeRedisCache(options => options.InstanceName = "taskboard:");
        services.AddOptions<RedisCacheOptions>()
            .Configure<IConnectionMultiplexer>((options, redis) =>
                options.ConnectionMultiplexerFactory = () => Task.FromResult(redis));

        services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis", tags: [ReadinessTag], timeout: HealthCheckTimeout);

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddScoped<InsertOutboxMessagesInterceptor>();
        services.AddSingleton<BoardRoleCacheInvalidationInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) => options
            .UseNpgsql(configuration.GetConnectionString("Database"))
            .AddInterceptors(
                serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>(),
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<BoardRoleCacheInvalidationInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IReadDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<IChecklistItemRepository, ChecklistItemRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();

        services.AddScoped<BoardAccessService>();
        services.AddScoped<IBoardAccessService, CachedBoardAccessService>();

        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<DomainEventContext>();
        services.AddScoped<IDomainEventContext>(provider => provider.GetRequiredService<DomainEventContext>());
        services.AddHostedService<OutboxProcessor>();

        services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database", tags: [ReadinessTag]);

        // AddDbContextCheck takes no timeout, so set it on the registration it created.
        services.Configure<HealthCheckServiceOptions>(options =>
            options.Registrations.Single(r => r.Name == "database").Timeout = HealthCheckTimeout);

        return services;
    }

    private static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SigningKey),
                "Jwt:SigningKey is not configured. Set the Jwt__SigningKey environment variable " +
                "(or 'dotnet user-secrets set Jwt:SigningKey <key>' in development) to a random value of at least 32 characters.")
            .ValidateOnStart();

        services.AddSingleton<ITokenProvider, JwtTokenProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.Configure<PasswordResetOptions>(configuration.GetSection(PasswordResetOptions.SectionName));
        services.AddSingleton<IPasswordResetTokenProvider, PasswordResetTokenProvider>();
        services.AddHostedService<ExpiredRefreshTokenCleanupService>();
        services.AddSingleton<IEmailVerificationTokens, EmailVerificationTokens>();

        return services;
    }

    private static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(FileStorageOptions.SectionName);
        services.Configure<FileStorageOptions>(section);

        var settings = section.Get<FileStorageOptions>() ?? new FileStorageOptions();
        if (string.Equals(settings.Provider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton(_ => new BlobServiceClient(settings.AzureConnectionString));
            services.AddScoped<IFileStorage, AzureBlobFileStorage>();
            services.AddHealthChecks()
                .AddCheck<AzureBlobStorageHealthCheck>("storage", tags: [ReadinessTag], timeout: HealthCheckTimeout);
        }
        else
        {
            services.AddScoped<IFileStorage, LocalFileStorage>();
            services.AddHealthChecks()
                .AddCheck<LocalFileStorageHealthCheck>("storage", tags: [ReadinessTag], timeout: HealthCheckTimeout);
        }

        services.AddHostedService<OrphanedFileCleanupService>();

        return services;
    }

    private static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(
                options => !options.Enabled || IsWebAddress(options.WebAppUrl),
                "Email:WebAppUrl must be the http(s) address of the web app when Email:Enabled is true.")
            .Validate(
                options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Smtp.Host),
                "Email:Smtp:Host is required when Email:Enabled is true.")
            .Validate(
                options => !options.Enabled || !string.IsNullOrWhiteSpace(options.FromAddress),
                "Email:FromAddress is required when Email:Enabled is true.")
            .ValidateOnStart();

        services.AddSingleton<IWebAppLinks, WebAppLinks>();

        services.AddSingleton<QueuedEmailSender>();
        services.AddSingleton<IEmailSender>(provider => provider.GetRequiredService<QueuedEmailSender>());
        services.AddHostedService<EmailDeliveryService>();

        services.Configure<DueReminderOptions>(configuration.GetSection(DueReminderOptions.SectionName));
        services.AddHostedService<CardDueReminderService>();

        var enabled = configuration.GetValue<bool>($"{EmailOptions.SectionName}:{nameof(EmailOptions.Enabled)}");
        if (enabled)
        {
            services.AddSingleton<IEmailTransport, SmtpEmailTransport>();
        }
        else
        {
            services.AddSingleton<IEmailTransport, LoggingEmailTransport>();
        }

        return services;
    }

    private static bool IsWebAddress(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
