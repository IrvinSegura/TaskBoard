using Microsoft.Extensions.Logging.Abstractions;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Teams.EventHandlers;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Teams.Events;

namespace TaskBoard.Application.UnitTests.Teams;

public class TeamMemberRemovedEventHandlerTests
{
    private readonly IBoardRepository _boards = Substitute.For<IBoardRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private TeamMemberRemovedEventHandler CreateHandler() =>
        new(_boards, _unitOfWork, NullLogger<TeamMemberRemovedEventHandler>.Instance);

    [Fact]
    public async Task Handle_RemovesUserFromTeamBoardsAndSaves()
    {
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var first = Board.Create(teamId, "First", Guid.NewGuid(), BoardVisibility.Private);
        var second = Board.Create(teamId, "Second", Guid.NewGuid(), BoardVisibility.Private);
        first.AddMember(userId, BoardRole.Editor);
        second.AddMember(userId, BoardRole.Viewer);

        _boards.GetTeamBoardsWithMemberAsync(teamId, userId, Arg.Any<CancellationToken>()).Returns([first, second]);

        await CreateHandler().Handle(new TeamMemberRemovedEvent(teamId, userId), CancellationToken.None);

        first.Members.ShouldNotContain(m => m.UserId == userId);
        second.Members.ShouldNotContain(m => m.UserId == userId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserIsLastBoardAdmin_KeepsThatMembershipAndRemovesTheRest()
    {
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var adminBoard = Board.Create(teamId, "Owned", userId, BoardVisibility.Private);
        var otherBoard = Board.Create(teamId, "Other", Guid.NewGuid(), BoardVisibility.Private);
        otherBoard.AddMember(userId, BoardRole.Editor);

        _boards.GetTeamBoardsWithMemberAsync(teamId, userId, Arg.Any<CancellationToken>())
            .Returns([adminBoard, otherBoard]);

        await CreateHandler().Handle(new TeamMemberRemovedEvent(teamId, userId), CancellationToken.None);

        adminBoard.Members.ShouldContain(m => m.UserId == userId);
        otherBoard.Members.ShouldNotContain(m => m.UserId == userId);
    }
}
