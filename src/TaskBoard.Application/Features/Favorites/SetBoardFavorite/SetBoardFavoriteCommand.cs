using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Messaging;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Features.Favorites.SetBoardFavorite;

// Stars or unstars a board for the current user. Setting the state it already has is a no-op.
public sealed record SetBoardFavoriteCommand(Guid BoardId, bool IsFavorite) : ICommand, IBoardScopedRequest
{
    BoardScope IBoardScopedRequest.Scope => BoardScope.Board;

    Guid IBoardScopedRequest.ScopeId => BoardId;

    BoardRole IBoardScopedRequest.MinimumRole => BoardRole.Viewer;

    // A star changes nothing on the board, so archived boards can be starred and unstarred too.
    bool IBoardScopedRequest.AllowsArchivedBoard => true;
}
