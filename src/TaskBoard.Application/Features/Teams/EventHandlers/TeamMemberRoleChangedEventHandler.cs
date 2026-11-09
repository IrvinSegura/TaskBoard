using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Domain.Notifications;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Teams.Events;

namespace TaskBoard.Application.Features.Teams.EventHandlers;

/// <summary>
/// Tells a member that their role in a team changed, since it changes what they can manage there.
/// </summary>
internal sealed class TeamMemberRoleChangedEventHandler(IReadDbContext db, NotificationPublisher publisher)
    : IDomainEventHandler<TeamMemberRoleChangedEvent>
{
    public async Task Handle(TeamMemberRoleChangedEvent domainEvent, CancellationToken cancellationToken)
    {
        var teamName = await db.Teams
            .Where(t => t.Id == domainEvent.TeamId)
            .Select(t => t.Name)
            .SingleOrDefaultAsync(cancellationToken);
        if (teamName is null)
        {
            return;
        }

        var actorName = await db.Users
            .Where(u => u.Id == domainEvent.ChangedById)
            .Select(u => u.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);

        await publisher.PublishAsync(
            domainEvent.UserId,
            NotificationType.TeamRoleChanged,
            $"Your role in the team \"{teamName}\" is now {EnglishName(domainEvent.Role)}.",
            domainEvent.TeamId,
            NotificationEmailFacts.TeamRole(domainEvent.TeamId, teamName, SpanishName(domainEvent.Role), actorName),
            cancellationToken);
    }

    // The in-app message is English (the web translates it); the email is written in Spanish.
    private static string EnglishName(TeamRole role) => role == TeamRole.Admin ? "admin" : "member";

    private static string SpanishName(TeamRole role) => role == TeamRole.Admin ? "administrador" : "miembro";
}
