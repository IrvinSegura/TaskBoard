using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Boards.AddBoardMember;

public sealed record AddBoardMemberCommand(Guid BoardId, Guid UserId, BoardRole Role) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Board;

    Guid IBoardScopedRequest.ScopeId => BoardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Admin;
}
