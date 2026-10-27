using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Application.Features.Labels.UpdateLabel;

public sealed record UpdateLabelCommand(Guid BoardId, Guid LabelId, string Name, LabelColor Color) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Board;

    Guid IBoardScopedRequest.ScopeId => BoardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
