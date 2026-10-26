using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Cards;

namespace TaskBoard.Application.Features.Cards.UpdateCard;

public sealed record UpdateCardCommand(
    Guid CardId,
    string Title,
    string? Description,
    CardPriority Priority,
    DateTimeOffset? DueDate)
    : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Card;

    Guid IBoardScopedRequest.ScopeId => CardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
