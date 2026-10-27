using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Infrastructure.Persistence.Repositories;

internal sealed class BoardRepository(ApplicationDbContext db) : IBoardRepository
{
    public Task<Board?> GetByIdAsync(Guid boardId, CancellationToken cancellationToken) =>
        db.Boards
            .Include(b => b.Members)
            .Include(b => b.Columns)
            .AsSplitQuery()
            .SingleOrDefaultAsync(b => b.Id == boardId, cancellationToken);

    public Task<Column?> GetColumnAsync(Guid columnId, CancellationToken cancellationToken) =>
        db.Columns.SingleOrDefaultAsync(c => c.Id == columnId, cancellationToken);

    public async Task<IReadOnlyList<Board>> GetTeamBoardsWithMemberAsync(
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken) =>
        await db.Boards
            .Include(b => b.Members)
            .Where(b => b.TeamId == teamId && b.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetTeamBoardIdsAsync(Guid teamId, CancellationToken cancellationToken) =>
        await db.Boards
            .Where(b => b.TeamId == teamId)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

    public void Add(Board board) => db.Boards.Add(board);

    public void Remove(Board board) => db.Boards.Remove(board);
}
