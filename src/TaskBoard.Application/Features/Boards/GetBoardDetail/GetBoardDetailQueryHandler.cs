using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Boards.GetBoardDetail;

internal sealed class GetBoardDetailQueryHandler(IReadDbContext db)
    : IQueryHandler<GetBoardDetailQuery, BoardDetailResponse>
{
    public async Task<Result<BoardDetailResponse>> Handle(GetBoardDetailQuery query, CancellationToken cancellationToken)
    {
        var board = await db.Boards
            .Where(b => b.Id == query.BoardId)
            .Select(b => new { b.Id, b.Name, b.Visibility })
            .SingleOrDefaultAsync(cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(query.BoardId);
        }

        var members = await (
            from member in db.BoardMembers
            join user in db.Users on member.UserId equals user.Id
            where member.BoardId == query.BoardId
            select new BoardMemberResponse(user.Id, user.DisplayName, member.Role))
            .ToListAsync(cancellationToken);

        var columns = await db.Columns
            .Where(c => c.BoardId == query.BoardId)
            .OrderBy(c => c.Position)
            .Select(c => new { c.Id, c.Name, c.Position })
            .ToListAsync(cancellationToken);

        var cards = await db.Cards
            .Where(c => c.BoardId == query.BoardId && !c.IsArchived)
            .OrderBy(c => c.Position)
            .Select(c => new
            {
                c.ColumnId,
                Card = new CardSummaryResponse(
                    c.Id,
                    c.Title,
                    c.Priority,
                    c.DueDate,
                    c.Position,
                    c.Assignments.Select(a => a.UserId).ToList())
            })
            .ToListAsync(cancellationToken);

        var cardsByColumn = cards.ToLookup(x => x.ColumnId, x => x.Card);

        return new BoardDetailResponse(
            board.Id,
            board.Name,
            board.Visibility,
            members,
            columns.Select(c => new ColumnResponse(c.Id, c.Name, c.Position, cardsByColumn[c.Id].ToList())).ToList());
    }
}
