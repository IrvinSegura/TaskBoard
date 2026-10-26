using Microsoft.Extensions.Logging;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Abstractions.Realtime;
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
    }
}
