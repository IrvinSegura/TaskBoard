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

    public void Add(Board board) => db.Boards.Add(board);
}
