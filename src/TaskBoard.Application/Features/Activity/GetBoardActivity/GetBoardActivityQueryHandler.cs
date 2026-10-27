using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Activity.GetBoardActivity;

internal sealed class GetBoardActivityQueryHandler(IReadDbContext db)
    : IQueryHandler<GetBoardActivityQuery, PagedResponse<ActivityResponse>>
{
    public async Task<Result<PagedResponse<ActivityResponse>>> Handle(GetBoardActivityQuery query, CancellationToken cancellationToken)
    {
        return await db.Activities
            .Where(a => a.BoardId == query.BoardId)
            .ToFeedAsync(db, query.Page, cancellationToken);
    }
}
