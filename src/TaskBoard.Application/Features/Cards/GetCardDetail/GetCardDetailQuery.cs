using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Cards.GetCardDetail;

public sealed record GetCardDetailQuery(Guid CardId) : IQuery<CardDetailResponse>, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Card;

    Guid IBoardScopedRequest.ScopeId => CardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Viewer;
}
