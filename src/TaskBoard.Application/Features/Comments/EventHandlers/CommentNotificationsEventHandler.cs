using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Domain.Comments;
using TaskBoard.Domain.Comments.Events;
using TaskBoard.Domain.Notifications;

namespace TaskBoard.Application.Features.Comments.EventHandlers;

/// <summary>
/// Tells the people mentioned in a new comment, and the card's assignees, that someone commented.
/// The author never gets either, and a mentioned assignee only gets the mention.
/// </summary>
internal sealed class CommentNotificationsEventHandler(
    IReadDbContext db,
    IBoardAccessService boardAccess,
    NotificationPublisher publisher)
    : IDomainEventHandler<CommentAddedEvent>
{
    public async Task Handle(CommentAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        var card = await db.Cards
            .Where(c => c.Id == domainEvent.CardId)
            .Select(c => new { c.Title, AssigneeIds = c.Assignments.Select(a => a.UserId).ToList() })
            .SingleOrDefaultAsync(cancellationToken);
        if (card is null)
        {
            return;
        }

        var authorName = await db.Users
            .Where(u => u.Id == domainEvent.AuthorId)
            .Select(u => u.DisplayName)
            .SingleOrDefaultAsync(cancellationToken) ?? "Someone";

        var mentioned = new HashSet<Guid>();
        foreach (var userId in CommentMentions.UserIds(domainEvent.Body))
        {
            // A mention only reaches people who can open the card.
            if (userId != domainEvent.AuthorId &&
                await boardAccess.GetRoleAsync(domainEvent.BoardId, userId, cancellationToken) is not null)
            {
                mentioned.Add(userId);
                await publisher.PublishAsync(
                    userId,
                    NotificationType.Mentioned,
                    $"{authorName} mentioned you in \"{card.Title}\".",
                    domainEvent.CardId,
                    cancellationToken);
            }
        }

        foreach (var assigneeId in card.AssigneeIds.Where(id => id != domainEvent.AuthorId && !mentioned.Contains(id)))
        {
            await publisher.PublishAsync(
                assigneeId,
                NotificationType.CommentAdded,
                $"{authorName} commented on \"{card.Title}\".",
                domainEvent.CardId,
                cancellationToken);
        }
    }
}
