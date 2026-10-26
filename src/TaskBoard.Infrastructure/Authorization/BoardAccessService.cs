using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Domain.Boards;
using TaskBoard.Infrastructure.Persistence;

namespace TaskBoard.Infrastructure.Authorization;

// TODO: cache roles for a few seconds if this becomes hot; invalidate on membership changes.
internal sealed class BoardAccessService(ApplicationDbContext db) : IBoardAccessService
{
    public async Task<BoardRole?> GetRoleAsync(Guid boardId, Guid userId, CancellationToken cancellationToken)
    {
        var board = await db.Boards
            .AsNoTracking()
            .Where(b => b.Id == boardId)
            .Select(b => new
            {
                b.TeamId,
                b.Visibility,
                Role = b.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (BoardRole?)m.Role)
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (board is null)
        {
            return null;
        }

        if (board.Role is not null)
        {
            return board.Role;
        }

        // Team-visible boards are readable by every member of the team.
        if (board.Visibility == BoardVisibility.Team &&
            await db.TeamMembers.AnyAsync(m => m.TeamId == board.TeamId && m.UserId == userId, cancellationToken))
        {
            return BoardRole.Viewer;
        }

        return null;
    }

    public Task<bool> IsArchivedAsync(Guid boardId, CancellationToken cancellationToken) =>
        db.Boards.AnyAsync(b => b.Id == boardId && b.IsArchived, cancellationToken);

    public Task<Guid?> ResolveBoardIdAsync(BoardScope scope, Guid scopeId, CancellationToken cancellationToken) =>
        scope switch
        {
            BoardScope.Board => db.Boards
                .Where(b => b.Id == scopeId).Select(b => (Guid?)b.Id).SingleOrDefaultAsync(cancellationToken),
            BoardScope.Column => db.Columns
                .Where(c => c.Id == scopeId).Select(c => (Guid?)c.BoardId).SingleOrDefaultAsync(cancellationToken),
            BoardScope.Card => db.Cards
                .Where(c => c.Id == scopeId).Select(c => (Guid?)c.BoardId).SingleOrDefaultAsync(cancellationToken),
            BoardScope.Comment => db.Comments
                .Where(c => c.Id == scopeId).Select(c => (Guid?)c.BoardId).SingleOrDefaultAsync(cancellationToken),
            BoardScope.Attachment => db.Attachments
                .Where(a => a.Id == scopeId).Select(a => (Guid?)a.BoardId).SingleOrDefaultAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown board scope.")
        };
}
