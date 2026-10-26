using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.UnitTests.Common;

public class PositionCalculatorTests
{
    [Theory]
    [InlineData(null, null, 1024)]
    [InlineData(null, 1024d, 512)]
    [InlineData(1024d, null, 2048)]
    [InlineData(1024d, 2048d, 1536)]
    public void Between_ReturnsPositionBetweenNeighbours(double? previous, double? next, double expected)
    {
        PositionCalculator.Between(previous, next).ShouldBe(expected);
    }
}
