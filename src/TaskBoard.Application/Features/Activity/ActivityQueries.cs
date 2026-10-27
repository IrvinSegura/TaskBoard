using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Domain.Activities;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Activity;

internal static class ActivityQueries
{
    // Newest first, with the actor's and the board's current names.
    public static Task<PagedResponse<ActivityResponse>> ToFeedAsync(
        this IQueryable<ActivityEntry> entries,
        IReadDbContext db,
        PageRequest page,
        CancellationToken cancellationToken) =>
        (from entry in entries
         join board in db.Boards on entry.BoardId equals board.Id
         join actor in db.Users on entry.ActorId equals actor.Id into actors
         from actor in actors.DefaultIfEmpty()
         orderby entry.OccurredAt descending, entry.Id descending
         select new ActivityResponse(
             entry.Id,
             entry.Type,
             entry.ActorId,
             actor == null ? null : actor.DisplayName,
             entry.BoardId,
             board.Name,
             entry.CardId,
             entry.Subject,
             entry.Detail,
             entry.OccurredAt))
        .ToPagedResponseAsync(page, cancellationToken);

    // Same rule as BoardAccessService: an explicit membership, or a team-visible board of one of the user's teams.
    public static IQueryable<Guid> VisibleBoardIds(this IReadDbContext db, Guid userId) =>
        db.Boards
            .Where(b => b.Members.Any(m => m.UserId == userId) ||
                        (b.Visibility == BoardVisibility.Team &&
                         db.TeamMembers.Any(m => m.TeamId == b.TeamId && m.UserId == userId)))
            .Select(b => b.Id);
}
