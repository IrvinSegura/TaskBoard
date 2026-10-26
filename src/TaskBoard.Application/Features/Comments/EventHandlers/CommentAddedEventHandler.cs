using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Comments.Events;

namespace TaskBoard.Application.Features.Comments.EventHandlers;

internal sealed class CommentAddedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<CommentAddedEvent>
{
    public Task Handle(CommentAddedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CommentAdded(
            new CommentAddedMessage(domainEvent.CommentId, domainEvent.CardId, domainEvent.AuthorId, domainEvent.Body));
}
