using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Columns.RenameColumn;

public sealed record RenameColumnCommand(Guid ColumnId, string Name) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Column;

    Guid IBoardScopedRequest.ScopeId => ColumnId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Editor;
}
