using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Columns.MoveColumn;

/// <summary>
/// Moves a column between the given neighbours (both null when it is the only column).
/// </summary>
public sealed record MoveColumnCommand(Guid ColumnId, Guid? PreviousColumnId, Guid? NextColumnId) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Column;

    Guid IBoardScopedRequest.ScopeId => ColumnId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
