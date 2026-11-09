using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Application.Features.Emails;
using TaskBoard.Application.Features.Notifications;
using TaskBoard.Domain.Notifications;
using TaskBoard.Domain.Teams.Events;

namespace TaskBoard.Application.Features.Teams.EventHandlers;

/// <summary>
/// A user who leaves a team also leaves its boards; otherwise explicit memberships
/// would keep granting access to private boards.
/// </summary>
internal sealed class TeamMemberRemovedEventHandler(
    IBoardRepository boards,
    IUnitOfWork unitOfWork,
    IBoardAccessService boardAccess,
    IRealtimeNotifier notifier,
    IReadDbContext db,
    NotificationPublisher publisher,
    ILogger<TeamMemberRemovedEventHandler> logger)
    : IDomainEventHandler<TeamMemberRemovedEvent>
{
    public async Task Handle(TeamMemberRemovedEvent domainEvent, CancellationToken cancellationToken)
    {
        var memberBoards = await boards.GetTeamBoardsWithMemberAsync(
            domainEvent.TeamId, domainEvent.UserId, cancellationToken);

        foreach (var board in memberBoards)
        {
            var result = board.RemoveMember(domainEvent.UserId);

            // The last admin stays so the board is not left unmanageable; another admin must be named first.
            if (result.IsFailure)
            {
                logger.LogWarning(
                    "User {UserId} left team {TeamId} but kept board {BoardId}: {Error}",
                    domainEvent.UserId,
                    domainEvent.TeamId,
                    board.Id,
                    result.Error.Code);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Team-visible boards were readable through team membership alone, with no member row to remove,
        // so drop the user's live connections from every team board they can no longer see.
        var teamBoardIds = await boards.GetTeamBoardIdsAsync(domainEvent.TeamId, cancellationToken);

        foreach (var boardId in teamBoardIds)
        {
            if (await boardAccess.GetRoleAsync(boardId, domainEvent.UserId, cancellationToken) is null)
            {
                await notifier.RemoveFromBoardAsync(boardId, domainEvent.UserId, cancellationToken);
            }
        }

        // Someone who left on their own needs no notice. The boards they lost go unmentioned: the team's
        // notice covers them.
        if (domainEvent.RemovedById is { } removedById && removedById != domainEvent.UserId)
        {
            await NotifyAsync(domainEvent, removedById, cancellationToken);
        }
    }

    private async Task NotifyAsync(TeamMemberRemovedEvent domainEvent, Guid removedById, CancellationToken cancellationToken)
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
            .Where(u => u.Id == removedById)
            .Select(u => u.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);

        await publisher.PublishAsync(
            domainEvent.UserId,
            NotificationType.RemovedFromTeam,
            $"You were removed from the team \"{teamName}\".",
            null,
            NotificationEmailFacts.RemovedFromTeam(teamName, actorName),
            cancellationToken);
    }
}
