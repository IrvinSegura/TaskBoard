using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Domain.Teams.Events;

namespace TaskBoard.Application.Features.Teams.EventHandlers;

internal sealed class TeamDeletedEventHandler(IRealtimeNotifier notifier) : IDomainEventHandler<TeamDeletedEvent>
{
    public async Task Handle(TeamDeletedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Whoever has one of its boards open leaves it.
        foreach (var boardId in domainEvent.BoardIds)
        {
            await notifier.Board(boardId).BoardDeleted(new BoardDeletedMessage(boardId));
        }

        // Every member drops the team from their sidebar, wherever they are in the app.
        var message = new TeamDeletedMessage(domainEvent.TeamId, domainEvent.Name);
        foreach (var memberId in domainEvent.MemberIds)
        {
            await notifier.User(memberId).TeamDeleted(message);
        }
    }
}
