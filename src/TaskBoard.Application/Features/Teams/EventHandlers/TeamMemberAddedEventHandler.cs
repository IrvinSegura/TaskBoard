using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Domain.Notifications;
using TaskBoard.Domain.Teams.Events;

namespace TaskBoard.Application.Features.Teams.EventHandlers;

internal sealed class TeamMemberAddedEventHandler(IReadDbContext db, NotificationPublisher publisher)
    : IDomainEventHandler<TeamMemberAddedEvent>
{
    public async Task Handle(TeamMemberAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        // The team may be gone by the time the outbox gets here; then there is nothing to point to.
        var teamName = await db.Teams
            .Where(t => t.Id == domainEvent.TeamId)
            .Select(t => t.Name)
            .SingleOrDefaultAsync(cancellationToken);

        if (teamName is null)
        {
            return;
        }

        await publisher.PublishAsync(
            domainEvent.UserId,
            NotificationType.AddedToTeam,
            $"You were added to the team \"{teamName}\".",
            domainEvent.TeamId,
            cancellationToken);
    }
}
