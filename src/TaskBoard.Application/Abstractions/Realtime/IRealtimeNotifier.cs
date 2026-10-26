namespace TaskBoard.Application.Abstractions.Realtime;

public interface IRealtimeNotifier
{
    // Everyone currently viewing the board.
    IBoardClient Board(Guid boardId);

    // Every open connection of one user.
    IBoardClient User(Guid userId);

    // Stops a user's open connections from receiving a board's events after losing access to it.
    Task RemoveFromBoardAsync(Guid boardId, Guid userId, CancellationToken cancellationToken);
}
