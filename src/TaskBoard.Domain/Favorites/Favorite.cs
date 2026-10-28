using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Favorites;

/// <summary>
/// A board or a team someone starred. Exactly one of <see cref="BoardId"/> and <see cref="TeamId"/> is set,
/// so the database can drop the star together with what it points at.
/// </summary>
public sealed class Favorite : Entity
{
    private Favorite()
    {
    }

    public Guid UserId { get; private set; }

    public Guid? BoardId { get; private set; }

    public Guid? TeamId { get; private set; }

    public static Favorite ForBoard(Guid userId, Guid boardId) => new() { UserId = userId, BoardId = boardId };

    public static Favorite ForTeam(Guid userId, Guid teamId) => new() { UserId = userId, TeamId = teamId };
}
