using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Domain.Boards.Events;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Boards.EventHandlers;

internal sealed class BoardMemberAddedEventHandler(IReadDbContext db, NotificationPublisher publisher)
    : IDomainEventHandler<BoardMemberAddedEvent>
{
    public async Task Handle(BoardMemberAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        var boardName = await db.Boards
            .Where(b => b.Id == domainEvent.BoardId)
            .Select(b => b.Name)
            .SingleAsync(cancellationToken);

        await publisher.PublishAsync(
            domainEvent.UserId,
            NotificationType.AddedToBoard,
            $"You were added to the board \"{boardName}\".",
            domainEvent.BoardId,
            cancellationToken);
    }
}
