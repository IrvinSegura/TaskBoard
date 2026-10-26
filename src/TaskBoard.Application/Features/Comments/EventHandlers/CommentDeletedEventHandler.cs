using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Comments.Events;

namespace TaskBoard.Application.Features.Comments.EventHandlers;

internal sealed class CommentDeletedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<CommentDeletedEvent>
{
    public Task Handle(CommentDeletedEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.Board(domainEvent.BoardId).CommentDeleted(
            new CommentDeletedMessage(domainEvent.CommentId, domainEvent.CardId));
}
