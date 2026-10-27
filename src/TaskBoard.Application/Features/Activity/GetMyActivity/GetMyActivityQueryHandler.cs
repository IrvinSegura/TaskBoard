using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Activity.GetMyActivity;

internal sealed class GetMyActivityQueryHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetMyActivityQuery, PagedResponse<ActivityResponse>>
{
    public async Task<Result<PagedResponse<ActivityResponse>>> Handle(GetMyActivityQuery query, CancellationToken cancellationToken)
    {
        var visible = db.VisibleBoardIds(currentUser.UserId);

        return await db.Activities
            .Where(a => visible.Contains(a.BoardId))
            .ToFeedAsync(db, query.Page, cancellationToken);
    }
}
