using TaskBoard.Domain.Labels;
using TaskBoard.Domain.Labels.Events;

namespace TaskBoard.Domain.UnitTests.Labels;

public class LabelTests
{
    [Fact]
    public void Update_ChangesNameAndColorAndRaisesLabelUpdatedEvent()
    {
        var label = Label.Create(Guid.NewGuid(), "Bug", LabelColor.Red);

        label.Update("  Defect ", LabelColor.Orange);

        label.Name.ShouldBe("Defect");
        label.Color.ShouldBe(LabelColor.Orange);
        label.DomainEvents.OfType<LabelUpdatedEvent>().ShouldHaveSingleItem().BoardId.ShouldBe(label.BoardId);
    }
}
