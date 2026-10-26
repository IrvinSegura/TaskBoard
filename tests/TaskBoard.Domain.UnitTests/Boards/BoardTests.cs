using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Boards.Events;

namespace TaskBoard.Domain.UnitTests.Boards;

public class BoardTests
{
    [Fact]
    public void Create_MakesCreatorAdmin()
    {
        var creatorId = Guid.NewGuid();

        var board = Board.Create(Guid.NewGuid(), "Roadmap", creatorId, BoardVisibility.Private);

        board.Members.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            m => m.UserId.ShouldBe(creatorId),
            m => m.Role.ShouldBe(BoardRole.Admin));
    }

    [Fact]
    public void RemoveMember_LastAdmin_ReturnsLastAdmin()
    {
        var creatorId = Guid.NewGuid();
        var board = Board.Create(Guid.NewGuid(), "Roadmap", creatorId, BoardVisibility.Private);

        var result = board.RemoveMember(creatorId);

        result.Error.ShouldBe(BoardErrors.LastAdmin);
    }

    [Fact]
    public void RemoveMember_NonAdmin_RemovesMemberAndRaisesBoardMemberRemovedEvent()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        var editorId = Guid.NewGuid();
        board.AddMember(editorId, BoardRole.Editor);

        var result = board.RemoveMember(editorId);

        result.IsSuccess.ShouldBeTrue();
        board.Members.ShouldNotContain(m => m.UserId == editorId);
        board.DomainEvents.OfType<BoardMemberRemovedEvent>().ShouldHaveSingleItem().UserId.ShouldBe(editorId);
    }

    [Fact]
    public void Rename_RaisesBoardRenamedEventWithTrimmedName()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);

        var result = board.Rename("  Roadmap 2027  ");

        result.IsSuccess.ShouldBeTrue();
        board.DomainEvents.OfType<BoardRenamedEvent>().ShouldHaveSingleItem().Name.ShouldBe("Roadmap 2027");
    }

    [Fact]
    public void Archive_Twice_RaisesSingleBoardArchivedEvent()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);

        board.Archive();
        board.Archive();

        board.IsArchived.ShouldBeTrue();
        board.DomainEvents.OfType<BoardArchivedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void RenameColumn_ExistingColumn_RenamesAndRaisesColumnRenamedEvent()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        var column = board.AddColumn("To do").Value;

        var result = board.RenameColumn(column.Id, "Backlog");

        result.IsSuccess.ShouldBeTrue();
        column.Name.ShouldBe("Backlog");
        board.DomainEvents.OfType<ColumnRenamedEvent>().ShouldHaveSingleItem().ColumnId.ShouldBe(column.Id);
    }

    [Fact]
    public void RenameColumn_ArchivedBoard_ReturnsArchived()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        var column = board.AddColumn("To do").Value;
        board.Archive();

        var result = board.RenameColumn(column.Id, "Backlog");

        result.Error.ShouldBe(BoardErrors.Archived);
    }

    [Fact]
    public void AddColumn_AppendsAfterLastColumn()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);

        var first = board.AddColumn("To do").Value;
        var second = board.AddColumn("Done").Value;

        second.Position.ShouldBeGreaterThan(first.Position);
    }
}
