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
    public void Update_ChangesFieldsAndRaisesCardUpdatedEventWithNewValues()
    {
        var card = CreateCard();
        var dueDate = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);

        var result = card.Update("  Ship it  ", "Details", CardPriority.High, dueDate);

        result.IsSuccess.ShouldBeTrue();
        card.Title.ShouldBe("Ship it");
        card.DomainEvents.OfType<CardUpdatedEvent>().ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            e => e.Title.ShouldBe("Ship it"),
            e => e.Priority.ShouldBe(CardPriority.High),
            e => e.DueDate.ShouldBe(dueDate));
    }

    [Fact]
    public void Update_ArchivedCard_ReturnsArchived()
    {
        var card = CreateCard();
        card.Archive();

        var result = card.Update("New title", null, CardPriority.Low, null);

        result.Error.ShouldBe(CardErrors.Archived);
    }

    [Fact]
    public void Archive_Twice_RaisesSingleCardArchivedEvent()
    {
        var card = CreateCard();

        card.Archive();
        card.Archive();

        card.IsArchived.ShouldBeTrue();
        card.DomainEvents.OfType<CardArchivedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Unassign_AssignedUser_RemovesAssignmentAndRaisesCardUnassignedEvent()
    {
        var card = CreateCard();
        var userId = Guid.NewGuid();
        card.Assign(userId, userId);

        var result = card.Unassign(userId);

        result.IsSuccess.ShouldBeTrue();
        card.Assignments.ShouldBeEmpty();
        card.DomainEvents.OfType<CardUnassignedEvent>().ShouldHaveSingleItem().UserId.ShouldBe(userId);
    }

    [Fact]
    public void Unassign_ArchivedCard_ReturnsArchived()
    {
        var card = CreateCard();
        var userId = Guid.NewGuid();
        card.Assign(userId, userId);
        card.Archive();

        var result = card.Unassign(userId);

        result.Error.ShouldBe(CardErrors.Archived);
    }

    [Fact]
    public void Restore_ArchivedCard_ReactivatesItInItsColumnAndRaisesCardRestoredEvent()
    {
        var card = CreateCard();
        card.Archive();

        card.Restore();

        card.IsArchived.ShouldBeFalse();
        card.DomainEvents.OfType<CardRestoredEvent>().ShouldHaveSingleItem().ColumnId.ShouldBe(card.ColumnId);
    }

    [Fact]
    public void MoveTo_ArchivedCard_ReturnsArchived()
    {
        var card = CreateCard();
        card.Archive();

        var result = card.MoveTo(Guid.NewGuid(), 2048);

        result.Error.ShouldBe(CardErrors.Archived);
    }

    [Fact]
    public void Delete_ActiveCard_ReturnsNotArchived()
    {
        var card = CreateCard();

        var result = card.Delete();

        result.Error.ShouldBe(CardErrors.NotArchived);
        card.DomainEvents.OfType<CardDeletedEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void Delete_ArchivedCard_RaisesCardDeletedEvent()
    {
        var card = CreateCard();
        card.Archive();

        var result = card.Delete();

        result.IsSuccess.ShouldBeTrue();
        card.DomainEvents.OfType<CardDeletedEvent>().ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            e => e.CardId.ShouldBe(card.Id),
            e => e.BoardId.ShouldBe(card.BoardId));
    }
}
