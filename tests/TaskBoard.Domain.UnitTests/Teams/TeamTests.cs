using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Teams.Events;

namespace TaskBoard.Domain.UnitTests.Teams;

public class TeamTests
{
    [Fact]
    public void RemoveMember_MemberLeavesOnTheirOwn_RaisesTeamMemberRemovedEvent()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var team = Team.Create("Platform", ownerId);
        team.AddMember(ownerId, memberId, TeamRole.Member);

        var result = team.RemoveMember(memberId, memberId);

        result.IsSuccess.ShouldBeTrue();
        team.IsMember(memberId).ShouldBeFalse();
        team.DomainEvents.OfType<TeamMemberRemovedEvent>().ShouldHaveSingleItem().UserId.ShouldBe(memberId);
    }

    [Fact]
    public void RemoveMember_MemberRemovesSomeoneElse_ReturnsNotAllowed()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var team = Team.Create("Platform", ownerId);
        team.AddMember(ownerId, memberId, TeamRole.Member);
        team.AddMember(ownerId, otherId, TeamRole.Member);

        var result = team.RemoveMember(memberId, otherId);

        result.Error.ShouldBe(TeamErrors.NotAllowed);
    }

    [Fact]
    public void RemoveMember_Owner_ReturnsCannotRemoveOwner()
    {
        var ownerId = Guid.NewGuid();
        var team = Team.Create("Platform", ownerId);

        var result = team.RemoveMember(ownerId, ownerId);

        result.Error.ShouldBe(TeamErrors.CannotRemoveOwner);
    }

    [Fact]
    public void ChangeMemberRole_ToOwner_ReturnsCannotAssignOwner()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var team = Team.Create("Platform", ownerId);
        team.AddMember(ownerId, memberId, TeamRole.Member);

        var result = team.ChangeMemberRole(ownerId, memberId, TeamRole.Owner);

        result.Error.ShouldBe(TeamErrors.CannotAssignOwner);
    }
}
