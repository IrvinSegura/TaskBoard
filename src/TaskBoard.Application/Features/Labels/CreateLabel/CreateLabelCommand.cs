using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Application.Features.Labels.CreateLabel;

public sealed record CreateLabelCommand(Guid BoardId, string Name, LabelColor Color) : ICommand<Guid>, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Board;

    Guid IBoardScopedRequest.ScopeId => BoardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
