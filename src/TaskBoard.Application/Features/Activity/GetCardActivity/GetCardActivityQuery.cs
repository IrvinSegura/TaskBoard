using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Activity.GetCardActivity;

// The history of one card, newest first.
public sealed record GetCardActivityQuery(Guid CardId, PageRequest Page)
    : IQuery<PagedResponse<ActivityResponse>>, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Card;

    Guid IBoardScopedRequest.ScopeId => CardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Viewer;
}
