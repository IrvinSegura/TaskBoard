using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Activity;
using TaskBoard.Domain.Cards;

namespace TaskBoard.Application.Features.Calendar.GetMyCalendar;

/// <summary>
/// What falls between <see cref="From"/> (inclusive) and <see cref="To"/> (exclusive) on every board the
/// user can see: the due dates of active cards and the activity. The caller picks the bounds in its own
/// time zone, so a month grid gets exactly its days.
/// </summary>
public sealed record GetMyCalendarQuery(DateTimeOffset From, DateTimeOffset To) : IQuery<CalendarResponse>;

/// <param name="ActivityTruncated">True when the range had more activity than one response carries; the newest is kept.</param>
public sealed record CalendarResponse(
    IReadOnlyList<CalendarCardResponse> Cards,
    IReadOnlyList<ActivityResponse> Activity,
    bool ActivityTruncated);

public sealed record CalendarCardResponse(
    Guid Id,
    string Title,
    CardPriority Priority,
    DateTimeOffset DueDate,
    Guid BoardId,
    string BoardName,
    string ColumnName,
    bool IsAssignedToMe);
