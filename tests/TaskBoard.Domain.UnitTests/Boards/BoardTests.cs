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
    public void MoveColumn_BetweenNeighbours_PlacesItBetweenThemAndRaisesColumnMovedEvent()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        var todo = board.AddColumn("To do").Value;
        var doing = board.AddColumn("Doing").Value;
        var done = board.AddColumn("Done").Value;

        var result = board.MoveColumn(done.Id, todo.Id, doing.Id);

        result.IsSuccess.ShouldBeTrue();
        done.Position.ShouldBeGreaterThan(todo.Position);
        done.Position.ShouldBeLessThan(doing.Position);
        board.DomainEvents.OfType<ColumnMovedEvent>().ShouldHaveSingleItem().ColumnId.ShouldBe(done.Id);
    }

    [Fact]
    public void MoveColumn_NeighbourFromAnotherBoard_ReturnsInvalidColumnNeighbour()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        var column = board.AddColumn("To do").Value;
        var otherBoard = Board.Create(Guid.NewGuid(), "Other", Guid.NewGuid(), BoardVisibility.Private);
        var foreignColumn = otherBoard.AddColumn("Elsewhere").Value;

        var result = board.MoveColumn(column.Id, foreignColumn.Id, null);

        result.Error.ShouldBe(BoardErrors.InvalidColumnNeighbour);
    }

    [Fact]
    public void MoveColumn_NextToItself_ReturnsInvalidColumnNeighbour()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        var column = board.AddColumn("To do").Value;

        var result = board.MoveColumn(column.Id, column.Id, null);

        result.Error.ShouldBe(BoardErrors.InvalidColumnNeighbour);
    }

    [Fact]
    public void MoveColumn_ArchivedBoard_ReturnsArchived()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        var column = board.AddColumn("To do").Value;
        board.Archive();

        var result = board.MoveColumn(column.Id, null, null);

        result.Error.ShouldBe(BoardErrors.Archived);
    }

    [Fact]
    public void Restore_ArchivedBoard_ReactivatesItAndRaisesBoardRestoredEvent()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        board.Archive();

        board.Restore();

        board.IsArchived.ShouldBeFalse();
        board.DomainEvents.OfType<BoardRestoredEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Restore_ActiveBoard_RaisesNoEvent()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);

        board.Restore();

        board.DomainEvents.OfType<BoardRestoredEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void AddColumn_AppendsAfterLastColumn()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);

        var first = board.AddColumn("To do").Value;
        var second = board.AddColumn("Done").Value;

        second.Position.ShouldBeGreaterThan(first.Position);
    }

    [Fact]
    public void MoveColumn_RepeatedlyIntoTheSameGap_RenumbersAndKeepsTheOrder()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        var first = board.AddColumn("First").Value;
        var a = board.AddColumn("A").Value;
        var b = board.AddColumn("B").Value;

        // Alternately moves A and B right after First, halving the same gap each time.
        var expected = new List<Column> { first, a, b };
        for (var i = 0; i < 60; i++)
        {
            var moving = i % 2 == 0 ? b : a;
            board.MoveColumn(moving.Id, first.Id, expected[1].Id).IsSuccess.ShouldBeTrue();
            expected.Remove(moving);
            expected.Insert(1, moving);
        }

        board.Columns.OrderBy(c => c.Position).ShouldBe(expected);
        var positions = board.Columns.Select(c => c.Position).Order().ToList();
        positions.Zip(positions.Skip(1), (previous, next) => next - previous).ShouldAllBe(gap => gap > 0);
        board.DomainEvents.OfType<ColumnMovedEvent>().Count().ShouldBeGreaterThan(60);
    }

    [Fact]
    public void Delete_ActiveBoard_ReturnsNotArchived()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);

        var result = board.Delete();

        result.Error.ShouldBe(BoardErrors.NotArchived);
        board.DomainEvents.OfType<BoardDeletedEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void Delete_ArchivedBoard_RaisesBoardDeletedEvent()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        board.Archive();

        var result = board.Delete();

        result.IsSuccess.ShouldBeTrue();
        board.DomainEvents.OfType<BoardDeletedEvent>().ShouldHaveSingleItem().BoardId.ShouldBe(board.Id);
    }

    [Fact]
    public void DeleteColumn_RemovesTheColumnAndRaisesColumnDeletedEvent()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        var column = board.AddColumn("To do").Value;
        var other = board.AddColumn("Done").Value;

        var result = board.DeleteColumn(column.Id);

        result.IsSuccess.ShouldBeTrue();
        board.Columns.ShouldHaveSingleItem().ShouldBe(other);
        board.DomainEvents.OfType<ColumnDeletedEvent>().ShouldHaveSingleItem().ColumnId.ShouldBe(column.Id);
    }

    [Fact]
    public void DeleteColumn_ArchivedBoard_ReturnsArchived()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);
        var column = board.AddColumn("To do").Value;
        board.Archive();

        var result = board.DeleteColumn(column.Id);

        result.Error.ShouldBe(BoardErrors.Archived);
        board.Columns.ShouldHaveSingleItem();
    }

    [Fact]
    public void DeleteColumn_UnknownColumn_ReturnsColumnNotFound()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);

        var result = board.DeleteColumn(Guid.NewGuid());

        result.Error.Code.ShouldBe("Boards.ColumnNotFound");
    }
}
