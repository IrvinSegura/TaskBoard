namespace TaskBoard.Application.Abstractions.Realtime;

public interface IRealtimeNotifier
{
    // Everyone currently viewing the board.
    IBoardClient Board(Guid boardId);

    // Every open connection of one user.
    IBoardClient User(Guid userId);
}
