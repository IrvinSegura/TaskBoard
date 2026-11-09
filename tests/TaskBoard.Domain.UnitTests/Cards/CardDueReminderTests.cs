using TaskBoard.Domain.Cards;

namespace TaskBoard.Domain.UnitTests.Cards;

public class CardDueReminderTests
{
    private static readonly DateTimeOffset Due = new(2026, 11, 5, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MarkDueReminderSent_RemembersTheDateItWasFor()
    {
        var card = Card.Create(Guid.NewGuid(), Guid.NewGuid(), "Demo", null, Guid.NewGuid(), 1);
        card.Update("Demo", null, CardPriority.None, Due);

        card.MarkDueReminderSent();

        card.DueReminderSentFor.ShouldBe(Due);
    }

    // A new date is a new deadline, so it gets its own reminder.
    [Fact]
    public void MovingTheDueDate_LeavesTheReminderForTheOldOne()
    {
        var card = Card.Create(Guid.NewGuid(), Guid.NewGuid(), "Demo", null, Guid.NewGuid(), 1);
        card.Update("Demo", null, CardPriority.None, Due);
        card.MarkDueReminderSent();

        card.Update("Demo", null, CardPriority.None, Due.AddDays(2));

        card.DueReminderSentFor.ShouldNotBe(card.DueDate);
    }
}
