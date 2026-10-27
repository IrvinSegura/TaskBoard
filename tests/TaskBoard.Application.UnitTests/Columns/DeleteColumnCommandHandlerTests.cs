using TaskBoard.Application.Abstractions.Persistence;
using TaskBoard.Application.Features.Columns.DeleteColumn;
using TaskBoard.Domain.Boards;
using TaskBoard.Domain.Common;

namespace TaskBoard.Application.UnitTests.Columns;

public class DeleteColumnCommandHandlerTests
{
    private readonly IBoardRepository _boards = Substitute.For<IBoardRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteColumnCommandHandler CreateHandler() => new(_boards, _unitOfWork);

    [Fact]
    public async Task Handle_ColumnDoesNotExist_ReturnsNotFound()
    {
        var result = await CreateHandler().Handle(new DeleteColumnCommand(Guid.NewGuid()), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingColumn_DeletesThroughBoardAndSaves()
    {
        var board = Board.Create(Guid.NewGuid(), "Board", Guid.NewGuid(), BoardVisibility.Private);
        var column = board.AddColumn("To do").Value;

        _boards.GetColumnAsync(column.Id, Arg.Any<CancellationToken>()).Returns(column);
        _boards.GetByIdAsync(board.Id, Arg.Any<CancellationToken>()).Returns(board);

        var result = await CreateHandler().Handle(new DeleteColumnCommand(column.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        board.Columns.ShouldBeEmpty();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
