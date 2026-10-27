using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Abstractions.Persistence;

public interface IBoardRepository
{
    // Loads the board with its members and columns.
    Task<Board?> GetByIdAsync(Guid boardId, CancellationToken cancellationToken);

    Task<Column?> GetColumnAsync(Guid columnId, CancellationToken cancellationToken);

    // Boards of the team where the user is an explicit member, loaded with their members.
    Task<IReadOnlyList<Board>> GetTeamBoardsWithMemberAsync(
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetTeamBoardIdsAsync(Guid teamId, CancellationToken cancellationToken);

    void Add(Board board);

    // The database deletes the board's columns, cards, comments and attachments with it.
    void Remove(Board board);
}
