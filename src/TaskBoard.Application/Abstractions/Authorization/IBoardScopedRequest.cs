using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.Abstractions.Authorization;

/// <summary>
/// Marks a command or query that touches a board resource. The authorization decorator
/// resolves the board from <see cref="Scope"/> and <see cref="ScopeId"/> and requires
/// the current user to hold at least <see cref="MinimumRole"/> on it. Commands are also
/// rejected on archived boards unless they set <see cref="AllowsArchivedBoard"/>.
/// </summary>
public interface IBoardScopedRequest
{
    BoardScope Scope { get; }

    Guid ScopeId { get; }

    BoardRole MinimumRole { get; }

    // Only for commands that manage the archive itself or access to it (archive, restore, remove member).
    bool AllowsArchivedBoard => false;
}
