using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Cards.RestoreCard;

public sealed record RestoreCardCommand(Guid CardId) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Card;

    Guid IBoardScopedRequest.ScopeId => CardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
