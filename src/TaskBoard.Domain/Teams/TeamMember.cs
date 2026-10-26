using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Teams;

public sealed class TeamMember : Entity
{
    private TeamMember()
    {
    }

    internal TeamMember(Guid teamId, Guid userId, TeamRole role)
    {
        TeamId = teamId;
        UserId = userId;
        Role = role;
    }

    public Guid TeamId { get; private set; }

    public Guid UserId { get; private set; }

    public TeamRole Role { get; private set; }

    internal void ChangeRole(TeamRole role) => Role = role;
}
