using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Teams.DeleteTeam;
using TaskBoard.Domain.Common;
using TaskBoard.Domain.Teams;
using TaskBoard.Domain.Teams.Events;

namespace TaskBoard.Application.UnitTests.Teams;

public class DeleteTeamCommandHandlerTests
{
    private readonly ITeamRepository _teams = Substitute.For<ITeamRepository>();
    private readonly IBoardRepository _boards = Substitute.For<IBoardRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteTeamCommandHandler CreateHandler() => new(_teams, _boards, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_UserOutsideTheTeam_ReturnsNotFound()
    {
        var team = Team.Create("Platform", Guid.NewGuid());
        _teams.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _currentUser.UserId.Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(new DeleteTeamCommand(team.Id), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        _teams.DidNotReceive().Remove(Arg.Any<Team>());
    }

    [Fact]
    public async Task Handle_Admin_IsForbiddenAndDoesNotSave()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var team = Team.Create("Platform", ownerId);
        team.AddMember(ownerId, adminId, TeamRole.Admin);
        _teams.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _currentUser.UserId.Returns(adminId);

        var result = await CreateHandler().Handle(new DeleteTeamCommand(team.Id), CancellationToken.None);

        result.Error.ShouldBe(TeamErrors.OnlyOwnerCanDelete);
        _teams.DidNotReceive().Remove(Arg.Any<Team>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Owner_RemovesTheTeamAndNamesItsBoardsInTheEvent()
    {
        var ownerId = Guid.NewGuid();
        var boardIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var team = Team.Create("Platform", ownerId);
        _teams.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _boards.GetTeamBoardIdsAsync(team.Id, Arg.Any<CancellationToken>()).Returns(boardIds);
        _currentUser.UserId.Returns(ownerId);

        var result = await CreateHandler().Handle(new DeleteTeamCommand(team.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _teams.Received(1).Remove(team);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        team.DomainEvents.OfType<TeamDeletedEvent>().ShouldHaveSingleItem().BoardIds.ShouldBe(boardIds);
    }
}
