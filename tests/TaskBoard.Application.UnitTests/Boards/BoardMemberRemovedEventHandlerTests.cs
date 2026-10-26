using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Realtime;
using TaskBoard.Application.Features.Boards.EventHandlers;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Application.UnitTests.Boards;

public class BoardMemberRemovedEventHandlerTests
{
    private readonly IRealtimeNotifier _notifier = Substitute.For<IRealtimeNotifier>();
    private readonly IBoardAccessService _boardAccess = Substitute.For<IBoardAccessService>();

    private BoardMemberRemovedEventHandler CreateHandler() => new(_notifier, _boardAccess);

    [Fact]
    public async Task Handle_UserLostAllAccess_DropsTheirConnectionsFromTheBoard()
    {
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _boardAccess.GetRoleAsync(boardId, userId, Arg.Any<CancellationToken>()).Returns((BoardRole?)null);

        await CreateHandler().Handle(new BoardMemberRemovedEvent(boardId, userId), CancellationToken.None);

        Received.InOrder(() =>
        {
            _notifier.RemoveFromBoardAsync(boardId, userId, Arg.Any<CancellationToken>());
            _notifier.Board(boardId).BoardMemberRemoved(new BoardMemberRemovedMessage(boardId, userId));
        });
    }

    [Fact]
    public async Task Handle_UserStillSeesBoardThroughTeam_KeepsTheirConnections()
    {
        var boardId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _boardAccess.GetRoleAsync(boardId, userId, Arg.Any<CancellationToken>()).Returns(BoardRole.Viewer);

        await CreateHandler().Handle(new BoardMemberRemovedEvent(boardId, userId), CancellationToken.None);

        await _notifier.DidNotReceive().RemoveFromBoardAsync(boardId, userId, Arg.Any<CancellationToken>());
        await _notifier.Board(boardId).Received(1).BoardMemberRemoved(new BoardMemberRemovedMessage(boardId, userId));
    }
}
