using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IBoardRepository
{
    // Loads the board with its members and columns.
    Task<Board?> GetByIdAsync(Guid boardId, CancellationToken cancellationToken);

    Task<Column?> GetColumnAsync(Guid columnId, CancellationToken cancellationToken);

    void Add(Board board);
}
