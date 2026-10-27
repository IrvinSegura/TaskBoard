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

    [Fact]
    public void Rename_ByAdmin_TrimsTheName()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var team = Team.Create("Platform", ownerId);
        team.AddMember(ownerId, adminId, TeamRole.Admin);

        var result = team.Rename(adminId, "  Core  ");

        result.IsSuccess.ShouldBeTrue();
        team.Name.ShouldBe("Core");
    }

    [Fact]
    public void Rename_ByPlainMember_ReturnsNotAllowed()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var team = Team.Create("Platform", ownerId);
        team.AddMember(ownerId, memberId, TeamRole.Member);

        var result = team.Rename(memberId, "Core");

        result.Error.ShouldBe(TeamErrors.NotAllowed);
        team.Name.ShouldBe("Platform");
    }

    [Fact]
    public void Delete_ByAdmin_ReturnsOnlyOwnerCanDelete()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var team = Team.Create("Platform", ownerId);
        team.AddMember(ownerId, adminId, TeamRole.Admin);

        var result = team.Delete(adminId, []);

        result.Error.ShouldBe(TeamErrors.OnlyOwnerCanDelete);
        team.DomainEvents.OfType<TeamDeletedEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void Delete_ByOwner_RaisesTeamDeletedEventWithBoardsAndMembers()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var team = Team.Create("Platform", ownerId);
        team.AddMember(ownerId, memberId, TeamRole.Member);

        var result = team.Delete(ownerId, [boardId]);

        result.IsSuccess.ShouldBeTrue();
        var deleted = team.DomainEvents.OfType<TeamDeletedEvent>().ShouldHaveSingleItem();
        deleted.BoardIds.ShouldBe([boardId]);
        deleted.MemberIds.ShouldBe([ownerId, memberId], ignoreOrder: true);
    }
}
