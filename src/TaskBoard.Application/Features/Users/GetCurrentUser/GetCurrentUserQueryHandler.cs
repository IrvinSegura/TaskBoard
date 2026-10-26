using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Users;

namespace TaskBoard.Application.Features.Users.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    public async Task<Result<CurrentUserResponse>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new CurrentUserResponse(u.Id, u.Email, u.DisplayName, u.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        // An access token can outlive the account it was issued for.
        return user is null ? UserErrors.NotFound(userId) : user;
    }
}
