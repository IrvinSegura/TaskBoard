using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Boards.GetBoardDetail;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.GetMyCards;

internal sealed class GetMyCardsQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetMyCardsQuery, IReadOnlyList<MyCardResponse>>
{
    // Enough for a personal list; the page shows the most urgent ones first.
    private const int Limit = 200;

    public async Task<Result<IReadOnlyList<MyCardResponse>>> Handle(GetMyCardsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Only assignees can be assigned, so an explicit board membership is what keeps the card visible.
        var cards = await (
            from card in db.Cards
            join board in db.Boards on card.BoardId equals board.Id
            join column in db.Columns on card.ColumnId equals column.Id
            join team in db.Teams on board.TeamId equals team.Id
            where !card.IsArchived && !board.IsArchived
            where card.Assignments.Any(a => a.UserId == userId)
            where board.Members.Any(m => m.UserId == userId)
            orderby card.DueDate == null, card.DueDate, card.Title
            select new
            {
                card.Id,
                card.Title,
                card.Priority,
                card.DueDate,
                card.BoardId,
                BoardName = board.Name,
                ColumnName = column.Name,
                TeamName = team.Name,
                LabelIds = card.Labels.Select(l => l.LabelId).ToList()
            })
            .Take(Limit)
            .ToListAsync(cancellationToken);

        var cardIds = cards.Select(c => c.Id).ToList();
        var labelIds = cards.SelectMany(c => c.LabelIds).Distinct().ToList();

        var labels = await db.Labels
            .Where(l => labelIds.Contains(l.Id))
            .Select(l => new LabelResponse(l.Id, l.Name, l.Color))
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var checklists = await db.ChecklistItems
            .Where(i => cardIds.Contains(i.CardId))
            .GroupBy(i => i.CardId)
            .Select(g => new { CardId = g.Key, Done = g.Count(i => i.IsDone), Total = g.Count() })
            .ToDictionaryAsync(x => x.CardId, x => new ChecklistProgressResponse(x.Done, x.Total), cancellationToken);

        var result = cards
            .Select(c => new MyCardResponse(
                c.Id,
                c.Title,
                c.Priority,
                c.DueDate,
                c.BoardId,
                c.BoardName,
                c.ColumnName,
                c.TeamName,
                c.LabelIds.Where(labels.ContainsKey).Select(id => labels[id]).OrderBy(l => l.Name).ToList(),
                checklists.GetValueOrDefault(c.Id)))
            .ToList();

        return Result.Success<IReadOnlyList<MyCardResponse>>(result);
    }
}
