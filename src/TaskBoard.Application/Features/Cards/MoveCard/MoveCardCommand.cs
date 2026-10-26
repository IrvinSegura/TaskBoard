using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Cards.MoveCard;

/// <summary>
/// Moves a card to <paramref name="TargetColumnId"/>, placed between the given neighbours
/// (both null means an empty column).
/// </summary>
public sealed record MoveCardCommand(Guid CardId, Guid TargetColumnId, Guid? PreviousCardId, Guid? NextCardId)
    : ICommand<CardMovedResponse>, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Card;

    Guid IBoardScopedRequest.ScopeId => CardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
