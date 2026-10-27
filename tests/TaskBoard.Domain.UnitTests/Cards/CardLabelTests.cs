using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Cards.Events;
using TaskBoard.Domain.Labels;

namespace TaskBoard.Domain.UnitTests.Cards;

public class CardLabelTests
{
    private static readonly Guid BoardId = Guid.NewGuid();

    private static Card CreateCard() =>
        Card.Create(BoardId, Guid.NewGuid(), "Write tests", null, Guid.NewGuid(), 1024);

    [Fact]
    public void AddLabel_LabelOfTheBoard_AddsItAndRaisesCardLabelsChangedEvent()
    {
        var card = CreateCard();
        var label = Label.Create(BoardId, "Bug", LabelColor.Red);

        var result = card.AddLabel(label);

        result.IsSuccess.ShouldBeTrue();
        card.Labels.ShouldHaveSingleItem().LabelId.ShouldBe(label.Id);
        card.DomainEvents.OfType<CardLabelsChangedEvent>().ShouldHaveSingleItem().CardId.ShouldBe(card.Id);
    }

    [Fact]
    public void AddLabel_LabelOfAnotherBoard_ReturnsLabelNotOnBoard()
    {
        var card = CreateCard();
        var label = Label.Create(Guid.NewGuid(), "Bug", LabelColor.Red);

        var result = card.AddLabel(label);

        result.Error.ShouldBe(CardErrors.LabelNotOnBoard);
        card.Labels.ShouldBeEmpty();
    }

    [Fact]
    public void AddLabel_Twice_ReturnsLabelAlreadyAdded()
    {
        var card = CreateCard();
        var label = Label.Create(BoardId, "Bug", LabelColor.Red);
        card.AddLabel(label);

        var result = card.AddLabel(label);

        result.Error.ShouldBe(CardErrors.LabelAlreadyAdded);
    }

    [Fact]
    public void AddLabel_ArchivedCard_ReturnsArchived()
    {
        var card = CreateCard();
        card.Archive();

        var result = card.AddLabel(Label.Create(BoardId, "Bug", LabelColor.Red));

        result.Error.ShouldBe(CardErrors.Archived);
    }

    [Fact]
    public void RemoveLabel_LabelNotOnTheCard_ReturnsLabelNotAdded()
    {
        var card = CreateCard();

        var result = card.RemoveLabel(Guid.NewGuid());

        result.Error.ShouldBe(CardErrors.LabelNotAdded);
    }

    [Fact]
    public void RemoveLabel_RemovesItAndRaisesCardLabelsChangedEvent()
    {
        var card = CreateCard();
        var label = Label.Create(BoardId, "Bug", LabelColor.Red);
        card.AddLabel(label);
        card.ClearDomainEvents();

        var result = card.RemoveLabel(label.Id);

        result.IsSuccess.ShouldBeTrue();
        card.Labels.ShouldBeEmpty();
        card.DomainEvents.OfType<CardLabelsChangedEvent>().ShouldHaveSingleItem();
    }
}
