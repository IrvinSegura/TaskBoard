using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Checklists.UpdateChecklistItem;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Checklists;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.UnitTests.Checklists;

public class UpdateChecklistItemCommandHandlerTests
{
    private readonly ICardRepository _cards = Substitute.For<ICardRepository>();
    private readonly IChecklistItemRepository _checklist = Substitute.For<IChecklistItemRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdateChecklistItemCommandHandler CreateHandler() => new(_cards, _checklist, _unitOfWork);

    [Fact]
    public async Task Handle_ItemOfAnotherCard_ReturnsNotFound()
    {
        var item = ChecklistItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Item", 1024);
        _checklist.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await CreateHandler().Handle(
            new UpdateChecklistItemCommand(Guid.NewGuid(), item.Id, null, true), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        item.IsDone.ShouldBeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ArchivedCard_ReturnsArchived()
    {
        var card = Card.Create(Guid.NewGuid(), Guid.NewGuid(), "Card", null, Guid.NewGuid(), 1024);
        card.Archive();
        var item = ChecklistItem.Create(card.Id, card.BoardId, "Item", 1024);
        _checklist.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _cards.GetByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);

        var result = await CreateHandler().Handle(
            new UpdateChecklistItemCommand(card.Id, item.Id, null, true), CancellationToken.None);

        result.Error.ShouldBe(CardErrors.Archived);
        item.IsDone.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_ChecksTheItemOffAndSaves()
    {
        var card = Card.Create(Guid.NewGuid(), Guid.NewGuid(), "Card", null, Guid.NewGuid(), 1024);
        var item = ChecklistItem.Create(card.Id, card.BoardId, "Item", 1024);
        _checklist.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _cards.GetByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);

        var result = await CreateHandler().Handle(
            new UpdateChecklistItemCommand(card.Id, item.Id, null, true), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        item.IsDone.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
