namespace TaskBoard.Application.Abstractions.Realtime;

public interface IRealtimeNotifier
{
    // Everyone currently viewing the board.
    IBoardClient Board(Guid boardId);

    // Every open connection of one user.
    IBoardClient User(Guid userId);

    // Every open connection of each of these users.
    IBoardClient Users(IReadOnlyCollection<Guid> userIds);

    // After a user lost access to a board: sends BoardMemberRemoved to each of their open connections,
    // then stops those connections from receiving the board's events.
    Task RemoveFromBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken);
}
