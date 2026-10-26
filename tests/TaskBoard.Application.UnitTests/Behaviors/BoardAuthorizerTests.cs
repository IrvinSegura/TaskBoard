using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Behaviors;
using TaskBoard.Application.Features.Boards.ArchiveBoard;
using TaskBoard.Application.Features.Boards.GetBoardDetail;
using TaskBoard.Application.Features.Columns.CreateColumn;
using TaskBoard.Domain.Boards;

namespace TaskBoard.Application.UnitTests.Behaviors;

public class BoardAuthorizerTests
{
    private readonly IBoardAccessService _boardAccess = Substitute.For<IBoardAccessService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public BoardAuthorizerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _boardAccess.ResolveBoardIdAsync(BoardScope.Board, _boardId, Arg.Any<CancellationToken>())
            .Returns((Guid?)_boardId);
    }

    private BoardAuthorizer CreateAuthorizer() => new(_boardAccess, _currentUser);

    private void GivenRole(BoardRole? role, bool archived)
    {
        _boardAccess.GetRoleAsync(_boardId, _userId, Arg.Any<CancellationToken>()).Returns(role);
        _boardAccess.IsArchivedAsync(_boardId, Arg.Any<CancellationToken>()).Returns(archived);
    }

    [Fact]
    public async Task Command_OnArchivedBoard_ReturnsArchived()
    {
        GivenRole(BoardRole.Admin, archived: true);

        var error = await CreateAuthorizer().AuthorizeAsync(
            new CreateColumnCommand(_boardId, "To do"), isWrite: true, CancellationToken.None);

        error.ShouldBe(BoardErrors.Archived);
    }

    [Fact]
    public async Task Query_OnArchivedBoard_IsAllowed()
    {
        GivenRole(BoardRole.Viewer, archived: true);

        var error = await CreateAuthorizer().AuthorizeAsync(
            new GetBoardDetailQuery(_boardId), isWrite: false, CancellationToken.None);

        error.ShouldBeNull();
    }

    [Fact]
    public async Task CommandThatAllowsArchivedBoard_OnArchivedBoard_IsAllowed()
    {
        GivenRole(BoardRole.Admin, archived: true);

        var error = await CreateAuthorizer().AuthorizeAsync(
            new ArchiveBoardCommand(_boardId), isWrite: true, CancellationToken.None);

        error.ShouldBeNull();
    }

    [Fact]
    public async Task NonMember_OnArchivedBoard_GetsNotFoundRatherThanArchived()
    {
        GivenRole(null, archived: true);

        var error = await CreateAuthorizer().AuthorizeAsync(
            new CreateColumnCommand(_boardId, "To do"), isWrite: true, CancellationToken.None);

        error.ShouldBe(AuthorizationErrors.ResourceNotFound);
    }

    [Fact]
    public async Task Command_WithLowerRole_ReturnsInsufficientRole()
    {
        GivenRole(BoardRole.Viewer, archived: false);

        var error = await CreateAuthorizer().AuthorizeAsync(
            new CreateColumnCommand(_boardId, "To do"), isWrite: true, CancellationToken.None);

        error.ShouldBe(AuthorizationErrors.InsufficientRole);
    }
}
