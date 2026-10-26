using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Storage;
using TaskBoard.Infrastructure.Authentication;
using TaskBoard.Infrastructure.Authorization;
using TaskBoard.Infrastructure.Persistence;
using TaskBoard.Infrastructure.Persistence.Interceptors;
using TaskBoard.Infrastructure.Persistence.Outbox;
using TaskBoard.Infrastructure.Persistence.Repositories;
using TaskBoard.Infrastructure.Storage;

namespace TaskBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        return services
            .AddPersistence(configuration)
            .AddAuthenticationServices(configuration)
            .AddFileStorage(configuration);
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<InsertOutboxMessagesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) => options
            .UseNpgsql(configuration.GetConnectionString("Database"))
            .AddInterceptors(
                serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>(),
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IReadDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<IBoardAccessService, BoardAccessService>();

        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.AddScoped<DomainEventDispatcher>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }

    private static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITokenProvider, JwtTokenProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

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
        }
        else
        {
            services.AddScoped<IFileStorage, LocalFileStorage>();
        }

        return services;
    }
}
