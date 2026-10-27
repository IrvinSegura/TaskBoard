using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.Features.Activity.GetCardActivity;

internal sealed class GetCardActivityQueryHandler(IReadDbContext db)
    : IQueryHandler<GetCardActivityQuery, PagedResponse<ActivityResponse>>
{
    public async Task<Result<PagedResponse<ActivityResponse>>> Handle(GetCardActivityQuery query, CancellationToken cancellationToken)
    {
        return await db.Activities
            .Where(a => a.CardId == query.CardId)
            .ToFeedAsync(db, query.Page, cancellationToken);
    }
}
