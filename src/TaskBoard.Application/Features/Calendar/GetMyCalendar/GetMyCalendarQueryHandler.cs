using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Activity;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Calendar.GetMyCalendar;

internal sealed class GetMyCalendarQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetMyCalendarQuery, CalendarResponse>
{
    // A month grid shows six weeks; a little more leaves room for time zones.
    private static readonly TimeSpan MaxRange = TimeSpan.FromDays(45);

    private const int CardLimit = 500;
    private const int ActivityLimit = 1000;

    private static readonly Error InvalidRange = Error.Validation(
        "Calendar.InvalidRange",
        $"'to' must come after 'from', at most {MaxRange.TotalDays} days later.");

    public async Task<Result<CalendarResponse>> Handle(GetMyCalendarQuery query, CancellationToken cancellationToken)
    {
        if (query.To <= query.From || query.To - query.From > MaxRange)
        {
            return InvalidRange;
        }

        var userId = currentUser.UserId;
        var visible = db.VisibleBoardIds(userId);

        var cards = await (
            from card in db.Cards
            join board in db.Boards on card.BoardId equals board.Id
            join column in db.Columns on card.ColumnId equals column.Id
            where visible.Contains(card.BoardId) && !card.IsArchived && !board.IsArchived
            where card.DueDate >= query.From && card.DueDate < query.To
            orderby card.DueDate, card.Title, card.Id
            select new CalendarCardResponse(
                card.Id,
                card.Title,
                card.Priority,
                card.DueDate!.Value,
                card.BoardId,
                board.Name,
                column.Name,
                card.Assignments.Any(a => a.UserId == userId)))
            .Take(CardLimit)
            .ToListAsync(cancellationToken);

        // One extra row tells whether the range had more than the limit.
        var activity = await db.Activities
            .Where(a => visible.Contains(a.BoardId) && a.OccurredAt >= query.From && a.OccurredAt < query.To)
            .ToResponses(db)
            .Take(ActivityLimit + 1)
            .ToListAsync(cancellationToken);

        var truncated = activity.Count > ActivityLimit;
        if (truncated)
        {
            activity.RemoveAt(activity.Count - 1);
        }

        return new CalendarResponse(cards, activity, truncated);
    }
}
