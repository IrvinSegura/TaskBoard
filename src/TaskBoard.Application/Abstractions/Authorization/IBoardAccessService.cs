using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Abstractions.Authorization;

public interface IBoardAccessService
{
    // Null when the user cannot see the board at all.
    Task<BoardRole?> GetRoleAsync(Guid boardId, Guid userId, CancellationToken cancellationToken);

    // Null when the resource does not exist.
    Task<Guid?> ResolveBoardIdAsync(BoardScope scope, Guid scopeId, CancellationToken cancellationToken);
}
