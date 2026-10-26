using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Cards.Events;

namespace TaskBoard.Domain.UnitTests.Cards;

public class CardTests
{
    private static Card CreateCard() =>
        Card.Create(Guid.NewGuid(), Guid.NewGuid(), "Write tests", null, Guid.NewGuid(), 1024);

    [Fact]
    public void Create_RaisesCardCreatedEvent()
    {
        var card = CreateCard();

        card.DomainEvents.OfType<CardCreatedEvent>().ShouldHaveSingleItem().CardId.ShouldBe(card.Id);
    }

    [Fact]
    public void MoveTo_ChangesColumnAndRaisesCardMovedEvent()
    {
        var card = CreateCard();
        var targetColumnId = Guid.NewGuid();

        var result = card.MoveTo(targetColumnId, 2048);

        result.IsSuccess.ShouldBeTrue();
        card.ColumnId.ShouldBe(targetColumnId);
        card.DomainEvents.OfType<CardMovedEvent>().ShouldHaveSingleItem().ToColumnId.ShouldBe(targetColumnId);
    }

    [Fact]
    public void Assign_SameUserTwice_ReturnsAlreadyAssigned()
    {
        var card = CreateCard();
        var userId = Guid.NewGuid();
        card.Assign(userId, userId);

        var result = card.Assign(userId, userId);

        result.Error.ShouldBe(CardErrors.AlreadyAssigned);
    }

    [Fact]
    public void MoveTo_ArchivedCard_ReturnsArchived()
    {
        var card = CreateCard();
        card.Archive();

        var result = card.MoveTo(Guid.NewGuid(), 2048);

        result.Error.ShouldBe(CardErrors.Archived);
    }
}
