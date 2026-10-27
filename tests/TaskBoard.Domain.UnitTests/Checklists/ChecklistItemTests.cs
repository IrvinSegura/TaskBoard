using TaskBoard.Domain.Checklists;
using TaskBoard.Domain.Checklists.Events;

namespace TaskBoard.Domain.UnitTests.Checklists;

public class ChecklistItemTests
{
    private static ChecklistItem CreateItem() =>
        ChecklistItem.Create(Guid.NewGuid(), Guid.NewGuid(), "  Write the migration  ", 1024);

    [Fact]
    public void Create_TrimsTheTextAndStartsUndone()
    {
        var item = CreateItem();

        item.Text.ShouldBe("Write the migration");
        item.IsDone.ShouldBeFalse();
        item.DomainEvents.OfType<ChecklistItemAddedEvent>().ShouldHaveSingleItem().ItemId.ShouldBe(item.Id);
    }

    [Fact]
    public void Update_OnlyDone_KeepsTheText()
    {
        var item = CreateItem();

        item.Update(null, isDone: true);

        item.IsDone.ShouldBeTrue();
        item.Text.ShouldBe("Write the migration");
        item.DomainEvents.OfType<ChecklistItemUpdatedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Update_OnlyText_KeepsTheDoneState()
    {
        var item = CreateItem();
        item.Update(null, isDone: true);

        item.Update(" Run it ", null);

        item.Text.ShouldBe("Run it");
        item.IsDone.ShouldBeTrue();
    }
}
