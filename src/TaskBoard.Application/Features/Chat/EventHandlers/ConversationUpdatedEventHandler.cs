using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Chat.Events;

namespace TaskBoard.Application.Features.Chat.EventHandlers;

internal sealed class ConversationUpdatedEventHandler(IRealtimeNotifier notifier)
    : IDomainEventHandler<ConversationUpdatedEvent>
{
    public Task Handle(ConversationUpdatedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Users(domainEvent.UserIds).ConversationUpdated(new ConversationUpdatedMessage(domainEvent.ConversationId));
}
