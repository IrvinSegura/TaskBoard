using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Comments.Events;

namespace TaskBoard.Application.Features.Comments.EventHandlers;

internal sealed class CommentEditedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<CommentEditedEvent>
{
    public Task Handle(CommentEditedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CommentEdited(
            new CommentEditedMessage(domainEvent.CommentId, domainEvent.CardId, domainEvent.Body));
}
