using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Common;

namespace TaskBoard.Infrastructure.Persistence.Outbox;

internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider)
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypes = new();

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var handlerType = HandlerTypes.GetOrAdd(
            domainEvent.GetType(),
            eventType => typeof(IDomainEventHandler<>).MakeGenericType(eventType));

        var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.Handle))!;

        foreach (var handler in serviceProvider.GetServices(handlerType))
        {
            await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
        }
    }
}
