using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Cards.CreateCard;

public sealed record CreateCardCommand(Guid ColumnId, string Title, string? Description)
    : ICommand<Guid>, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Column;

    Guid IBoardScopedRequest.ScopeId => ColumnId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
