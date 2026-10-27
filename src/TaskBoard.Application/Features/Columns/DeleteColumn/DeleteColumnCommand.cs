using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Columns.DeleteColumn;

// Admins only: the column's cards are deleted with it.
public sealed record DeleteColumnCommand(Guid ColumnId) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Column;

    Guid IBoardScopedRequest.ScopeId => ColumnId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Admin;
}
