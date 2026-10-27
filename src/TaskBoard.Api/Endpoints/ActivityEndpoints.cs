using TaskBoard.Api.Extensions;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Application.Features.Activity;
using TaskBoard.Application.Features.Activity.GetBoardActivity;
using TaskBoard.Application.Features.Activity.GetCardActivity;
using TaskBoard.Application.Features.Activity.GetMyActivity;
using TaskBoard.Application.Features.Activity.GetTeamActivity;

namespace TaskBoard.Api.Endpoints;

internal sealed class ActivityEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty).WithTags("Activity").RequireAuthorization();

        group.MapGet("/me/activity", async (
            [AsParameters] PageQuery page,
            IQueryHandler<GetMyActivityQuery, PagedResponse<ActivityResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetMyActivityQuery(page.ToRequest()), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/teams/{teamId:guid}/activity", async (
            Guid teamId,
            [AsParameters] PageQuery page,
            IQueryHandler<GetTeamActivityQuery, PagedResponse<ActivityResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetTeamActivityQuery(teamId, page.ToRequest()), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/boards/{boardId:guid}/activity", async (
            Guid boardId,
            [AsParameters] PageQuery page,
            IQueryHandler<GetBoardActivityQuery, PagedResponse<ActivityResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetBoardActivityQuery(boardId, page.ToRequest()), cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/cards/{cardId:guid}/activity", async (
            Guid cardId,
            [AsParameters] PageQuery page,
            IQueryHandler<GetCardActivityQuery, PagedResponse<ActivityResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetCardActivityQuery(cardId, page.ToRequest()), cancellationToken);
            return result.ToHttpResult();
        });
    }

    internal sealed record PageQuery(int? Page, int? PageSize)
    {
        public PageRequest ToRequest() => PageRequest.Create(Page, PageSize);
    }
}
