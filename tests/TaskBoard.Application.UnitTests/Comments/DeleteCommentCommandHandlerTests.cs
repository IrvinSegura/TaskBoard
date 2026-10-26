using TaskBoard.Application.Abstractions.Authentication;
using TaskBoard.Application.Abstractions.Authorization;
using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Comments.DeleteComment;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Comments;

namespace TaskBoard.Application.UnitTests.Comments;

public class DeleteCommentCommandHandlerTests
{
    private readonly ICommentRepository _comments = Substitute.For<ICommentRepository>();
    private readonly IBoardAccessService _boardAccess = Substitute.For<IBoardAccessService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteCommentCommandHandler CreateHandler() => new(_comments, _boardAccess, _currentUser, _unitOfWork);

    private Comment GivenCommentByAnotherUser(BoardRole currentUserRole)
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Body");
        var currentUserId = Guid.NewGuid();

        _currentUser.UserId.Returns(currentUserId);
        _comments.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>()).Returns(comment);
        _boardAccess.GetRoleAsync(comment.BoardId, currentUserId, Arg.Any<CancellationToken>())
            .Returns(currentUserRole);

        return comment;
    }

    [Fact]
    public async Task Handle_BoardAdminDeletesSomeoneElsesComment_RemovesAndSaves()
    {
        var comment = GivenCommentByAnotherUser(BoardRole.Admin);

        var result = await CreateHandler().Handle(new DeleteCommentCommand(comment.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _comments.Received(1).Remove(comment);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EditorDeletesSomeoneElsesComment_ReturnsCannotDelete()
    {
        var comment = GivenCommentByAnotherUser(BoardRole.Editor);

        var result = await CreateHandler().Handle(new DeleteCommentCommand(comment.Id), CancellationToken.None);

        result.Error.ShouldBe(CommentErrors.CannotDelete);
        _comments.DidNotReceive().Remove(Arg.Any<Comment>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
