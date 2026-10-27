using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Activity.GetBoardActivity;

// Everything that happened on the board, newest first.
public sealed record GetBoardActivityQuery(Guid BoardId, PageRequest Page)
    : IQuery<PagedResponse<ActivityResponse>>, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Board;

    Guid IBoardScopedRequest.ScopeId => BoardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Viewer;
}
