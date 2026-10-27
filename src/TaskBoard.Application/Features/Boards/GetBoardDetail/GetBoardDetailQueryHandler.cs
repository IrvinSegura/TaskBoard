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

        var labels = await db.Labels
            .Where(l => l.BoardId == query.BoardId)
            .OrderBy(l => l.Name)
            .Select(l => new LabelResponse(l.Id, l.Name, l.Color))
            .ToListAsync(cancellationToken);

        var columns = await db.Columns
            .Where(c => c.BoardId == query.BoardId)
            .OrderBy(c => c.Position)
            .Select(c => new { c.Id, c.Name, c.Position })
            .ToListAsync(cancellationToken);

        // Grouped queries for the whole board instead of counts per card.
        var checklists = await db.ChecklistItems
            .Where(i => i.BoardId == query.BoardId)
            .GroupBy(i => i.CardId)
            .Select(g => new { CardId = g.Key, Done = g.Count(i => i.IsDone), Total = g.Count() })
            .ToDictionaryAsync(x => x.CardId, x => new ChecklistProgressResponse(x.Done, x.Total), cancellationToken);

        var commentCounts = await db.Comments
            .Where(c => c.BoardId == query.BoardId)
            .GroupBy(c => c.CardId)
            .Select(g => new { CardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CardId, x => x.Count, cancellationToken);

        var attachmentCounts = await db.Attachments
            .Where(a => a.BoardId == query.BoardId)
            .GroupBy(a => a.CardId)
            .Select(g => new { CardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CardId, x => x.Count, cancellationToken);

        var cards = await db.Cards
            .Where(c => c.BoardId == query.BoardId && !c.IsArchived)
            .OrderBy(c => c.Position)
            .Select(c => new
            {
                c.ColumnId,
                c.Id,
                c.Title,
                c.Priority,
                c.DueDate,
                c.Position,
                AssigneeIds = c.Assignments.Select(a => a.UserId).ToList(),
                LabelIds = c.Labels.Select(l => l.LabelId).ToList()
            })
            .ToListAsync(cancellationToken);

        var cardsByColumn = cards.ToLookup(
            x => x.ColumnId,
            x => new CardSummaryResponse(
                x.Id,
                x.Title,
                x.Priority,
                x.DueDate,
                x.Position,
                x.AssigneeIds,
                x.LabelIds,
                checklists.GetValueOrDefault(x.Id),
                commentCounts.GetValueOrDefault(x.Id),
                attachmentCounts.GetValueOrDefault(x.Id)));

        return new BoardDetailResponse(
            board.Id,
            board.Name,
            board.Visibility,
            members,
            labels,
            columns.Select(c => new ColumnResponse(c.Id, c.Name, c.Position, cardsByColumn[c.Id].ToList())).ToList());
    }
}
