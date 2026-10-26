using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Boards.RemoveBoardMember;

public sealed record RemoveBoardMemberCommand(Guid BoardId, Guid UserId) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Board;

    Guid IBoardScopedRequest.ScopeId => BoardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Admin;

    // Revoking access must keep working after the board is archived.
    bool IBoardScopedRequest.AllowsArchivedBoard => true;
}
