using TaskBoard.Domain.Boards;

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
    public void AddColumn_AppendsAfterLastColumn()
    {
        var board = Board.Create(Guid.NewGuid(), "Roadmap", Guid.NewGuid(), BoardVisibility.Private);

        var first = board.AddColumn("To do").Value;
        var second = board.AddColumn("Done").Value;

        second.Position.ShouldBeGreaterThan(first.Position);
    }
}
