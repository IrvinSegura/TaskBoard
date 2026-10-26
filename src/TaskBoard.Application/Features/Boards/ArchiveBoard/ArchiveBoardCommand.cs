using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Boards.ArchiveBoard;

public sealed record ArchiveBoardCommand(Guid BoardId) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Board;

    Guid IBoardScopedRequest.ScopeId => BoardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Admin;

    bool IBoardScopedRequest.AllowsArchivedBoard => true;
}
