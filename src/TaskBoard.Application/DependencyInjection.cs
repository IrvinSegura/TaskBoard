using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Behaviors;
using TaskBoard.Application.Features.Notifications;

namespace TaskBoard.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Decorators are open generic types; skip them so they are not registered as handlers.
        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<>)).Where(IsClosedType), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)).Where(IsClosedType), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)).Where(IsClosedType), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)).Where(IsClosedType), publicOnly: false)
                .AsImplementedInterfaces().WithScopedLifetime());

        // Pipeline, outermost last: Logging -> Authorization -> Validation -> Handler.
        services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandHandler<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));

        services.TryDecorate(typeof(ICommandHandler<>), typeof(BoardAuthorizationDecorator.CommandHandler<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(BoardAuthorizationDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(BoardAuthorizationDecorator.QueryHandler<,>));

        services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandHandler<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddScoped<BoardAuthorizer>();
        services.AddScoped<NotificationPublisher>();

        return services;
    }

    private static bool IsClosedType(Type type) => !type.IsGenericTypeDefinition;
}
