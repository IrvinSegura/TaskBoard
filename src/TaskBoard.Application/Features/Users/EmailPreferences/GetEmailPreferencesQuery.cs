using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Users.EmailPreferences;

public sealed record GetEmailPreferencesQuery : IQuery<EmailPreferencesResponse>;

internal sealed class GetEmailPreferencesQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetEmailPreferencesQuery, EmailPreferencesResponse>
{
    public async Task<Result<EmailPreferencesResponse>> Handle(
        GetEmailPreferencesQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var muted = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.MutedEmailNotifications)
            .SingleOrDefaultAsync(cancellationToken);

        return muted is null ? UserErrors.NotFound(userId) : new EmailPreferencesResponse(muted);
    }
}
