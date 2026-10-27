using TaskBoard.Domain.Common;
using TaskBoard.Domain.Teams.Events;

namespace TaskBoard.Domain.Teams;

public sealed class Team : AggregateRoot
{
    private readonly List<TeamMember> _members = [];

    private Team()
    {
    }

    public string Name { get; private set; } = null!;

    public IReadOnlyCollection<TeamMember> Members => _members.AsReadOnly();

    public static Team Create(string name, Guid ownerId)
    {
        var team = new Team { Name = name.Trim() };
        team._members.Add(new TeamMember(team.Id, ownerId, TeamRole.Owner));
        return team;
    }

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public bool CanManageMembers(Guid userId) =>
        _members.Any(m => m.UserId == userId && m.Role >= TeamRole.Admin);

    public Result Rename(Guid actorId, string name)
    {
        if (!CanManageMembers(actorId))
        {
            return TeamErrors.NotAllowed;
        }

        Name = name.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Only the owner can delete a team. The database removes its boards with it, so the caller passes their ids
    /// for the event: once the team is gone nothing else knows which boards it had.
    /// </summary>
    public Result Delete(Guid actorId, IReadOnlyList<Guid> boardIds)
    {
        if (!_members.Any(m => m.UserId == actorId && m.Role == TeamRole.Owner))
        {
            return TeamErrors.OnlyOwnerCanDelete;
        }

        Raise(new TeamDeletedEvent(Id, Name, boardIds, _members.Select(m => m.UserId).ToList()));
        return Result.Success();
    }

    public Result AddMember(Guid actorId, Guid userId, TeamRole role)
    {
        if (!CanManageMembers(actorId))
        {
            return TeamErrors.NotAllowed;
        }

        if (role == TeamRole.Owner)
        {
            return TeamErrors.CannotAssignOwner;
        }

        if (IsMember(userId))
        {
            return TeamErrors.AlreadyMember;
        }

        _members.Add(new TeamMember(Id, userId, role));
        Raise(new TeamMemberAddedEvent(Id, userId));
        return Result.Success();
    }

    public Result ChangeMemberRole(Guid actorId, Guid userId, TeamRole role)
    {
        if (!CanManageMembers(actorId))
        {
            return TeamErrors.NotAllowed;
        }

        var member = _members.SingleOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            return TeamErrors.MemberNotFound;
        }

        if (member.Role == TeamRole.Owner || role == TeamRole.Owner)
        {
            return TeamErrors.CannotAssignOwner;
        }

        member.ChangeRole(role);
        return Result.Success();
    }

    public Result RemoveMember(Guid actorId, Guid userId)
    {
        // Members can always leave a team on their own.
        if (actorId != userId && !CanManageMembers(actorId))
        {
            return TeamErrors.NotAllowed;
        }

        var member = _members.SingleOrDefault(m => m.UserId == userId);
        if (member is null)
        {
            return TeamErrors.MemberNotFound;
        }

        if (member.Role == TeamRole.Owner)
        {
            return TeamErrors.CannotRemoveOwner;
        }

        _members.Remove(member);
        Raise(new TeamMemberRemovedEvent(Id, userId));
        return Result.Success();
    }
}
