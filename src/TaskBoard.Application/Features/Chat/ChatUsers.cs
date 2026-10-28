using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Persistence;

namespace TaskBoard.Application.Features.Chat;

internal static class ChatUsers
{
    // The first id with no user behind it, or null when they all exist.
    public static async Task<Guid?> FindMissingAsync(
        IReadDbContext db,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var distinct = userIds.Distinct().ToList();
        var found = await db.Users.Where(u => distinct.Contains(u.Id)).Select(u => u.Id).ToListAsync(cancellationToken);
        return distinct.Except(found).Select(id => (Guid?)id).FirstOrDefault();
    }
}
