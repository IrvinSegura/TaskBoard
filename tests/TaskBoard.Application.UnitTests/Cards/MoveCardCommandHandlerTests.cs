using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Cards.MoveCard;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Cards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.UnitTests.Cards;

public class MoveCardCommandHandlerTests
{
    private readonly ICardRepository _cards = Substitute.For<ICardRepository>();
    private readonly IBoardRepository _boards = Substitute.For<IBoardRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private MoveCardCommandHandler CreateHandler() => new(_cards, _boards, _unitOfWork);

    [Fact]
    public async Task Handle_CardDoesNotExist_ReturnsNotFound()
    {
        var command = new MoveCardCommand(Guid.NewGuid(), Guid.NewGuid(), null, null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TargetColumnOnAnotherBoard_ReturnsInvalidTargetColumn()
    {
        var card = Card.Create(Guid.NewGuid(), Guid.NewGuid(), "Card", null, Guid.NewGuid(), 1024);
        var otherBoard = Board.Create(Guid.NewGuid(), "Other", Guid.NewGuid(), BoardVisibility.Private);
        var foreignColumn = otherBoard.AddColumn("To do").Value;

        _cards.GetByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);
        _boards.GetColumnAsync(foreignColumn.Id, Arg.Any<CancellationToken>()).Returns(foreignColumn);

        var result = await CreateHandler().Handle(
            new MoveCardCommand(card.Id, foreignColumn.Id, null, null), CancellationToken.None);

        result.Error.ShouldBe(CardErrors.InvalidTargetColumn);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidMove_PlacesCardBetweenNeighboursAndSaves()
    {
        var board = Board.Create(Guid.NewGuid(), "Board", Guid.NewGuid(), BoardVisibility.Private);
        var column = board.AddColumn("Doing").Value;
        var card = Card.Create(board.Id, Guid.NewGuid(), "Card", null, Guid.NewGuid(), 1024);
        var previousId = Guid.NewGuid();
        var nextId = Guid.NewGuid();

        _cards.GetByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);
        _boards.GetColumnAsync(column.Id, Arg.Any<CancellationToken>()).Returns(column);
        _cards.GetPositionInColumnAsync(previousId, column.Id, Arg.Any<CancellationToken>()).Returns((double?)1000d);
        _cards.GetPositionInColumnAsync(nextId, column.Id, Arg.Any<CancellationToken>()).Returns((double?)2000d);

        var result = await CreateHandler().Handle(
            new MoveCardCommand(card.Id, column.Id, previousId, nextId), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Position.ShouldBe(1500d);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
