using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Cards.SearchCards;

internal sealed class SearchCardsQueryHandler(IReadDbContext db)
    : IQueryHandler<SearchCardsQuery, PagedResponse<CardSearchResultResponse>>
{
    public async Task<Result<PagedResponse<CardSearchResultResponse>>> Handle(
        SearchCardsQuery query,
        CancellationToken cancellationToken)
    {
        var cards = db.Cards.Where(c => c.BoardId == query.BoardId && c.IsArchived == query.Archived);

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var text = query.Text.Trim().ToLower();
            cards = cards.Where(c =>
                c.Title.ToLower().Contains(text) ||
                (c.Description != null && c.Description.ToLower().Contains(text)));
        }

        if (query.ColumnId is { } columnId)
        {
            cards = cards.Where(c => c.ColumnId == columnId);
        }

        if (query.AssigneeId is { } assigneeId)
        {
            cards = cards.Where(c => c.Assignments.Any(a => a.UserId == assigneeId));
        }

        if (query.LabelId is { } labelId)
        {
            cards = cards.Where(c => c.Labels.Any(l => l.LabelId == labelId));
        }

        if (query.Priority is { } priority)
        {
            cards = cards.Where(c => c.Priority == priority);
        }

        if (query.DueBefore is { } dueBefore)
        {
            cards = cards.Where(c => c.DueDate != null && c.DueDate < dueBefore);
        }

        var results = await cards
            .OrderBy(c => c.DueDate == null)
            .ThenBy(c => c.DueDate)
            .ThenBy(c => c.Title)
            .ThenBy(c => c.Id)
            .Select(c => new CardSearchResultResponse(
                c.Id,
                c.ColumnId,
                c.Title,
                c.Priority,
                c.DueDate,
                c.IsArchived,
                c.Assignments.Select(a => a.UserId).ToList(),
                c.Labels.Select(l => l.LabelId).ToList()))
            .ToPagedResponseAsync(query.Page, cancellationToken);

        return results;
    }
}
