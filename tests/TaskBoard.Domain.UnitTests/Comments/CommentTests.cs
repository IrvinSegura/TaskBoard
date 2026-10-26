using TaskBoard.Domain.Comments;
using TaskBoard.Domain.Comments.Events;

namespace TaskBoard.Domain.UnitTests.Comments;

public class CommentTests
{
    [Fact]
    public void Edit_ByAuthor_ChangesBodyAndRaisesCommentEditedEvent()
    {
        var authorId = Guid.NewGuid();
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), authorId, "First draft");

        var result = comment.Edit(authorId, "  Final text  ");

        result.IsSuccess.ShouldBeTrue();
        comment.Body.ShouldBe("Final text");
        comment.IsEdited.ShouldBeTrue();
        comment.DomainEvents.OfType<CommentEditedEvent>().ShouldHaveSingleItem().Body.ShouldBe("Final text");
    }

    [Fact]
    public void Edit_ByAnotherUser_ReturnsNotAuthor()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "First draft");

        var result = comment.Edit(Guid.NewGuid(), "Hijacked");

        result.Error.ShouldBe(CommentErrors.NotAuthor);
        comment.Body.ShouldBe("First draft");
    }
}
