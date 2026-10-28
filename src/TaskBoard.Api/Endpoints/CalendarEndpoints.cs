using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Features.Calendar.GetMyCalendar;

namespace TaskBoard.Api.Endpoints;

internal sealed class CalendarEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // e.g. /me/calendar?from=2026-09-28T00:00:00-06:00&to=2026-11-09T00:00:00-06:00
        app.MapGet("/me/calendar", async (
            DateTimeOffset from,
            DateTimeOffset to,
            IQueryHandler<GetMyCalendarQuery, CalendarResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetMyCalendarQuery(from, to), cancellationToken);
            return result.ToHttpResult();
        })
        .WithTags("Calendar")
        .RequireAuthorization();
    }
}
