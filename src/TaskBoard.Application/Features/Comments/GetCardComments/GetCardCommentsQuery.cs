using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Application.Common.Paging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Comments.GetCardComments;

// Oldest first, so later pages hold the newer comments.
public sealed record GetCardCommentsQuery(Guid CardId, PageRequest Page)
    : IQuery<PagedResponse<CommentResponse>>, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Card;

    Guid IBoardScopedRequest.ScopeId => CardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Viewer;
}

public sealed record CommentResponse(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Body,
    bool IsEdited,
    DateTimeOffset CreatedAt);
