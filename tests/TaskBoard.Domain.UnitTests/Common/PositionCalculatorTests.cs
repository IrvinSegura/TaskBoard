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

    [Theory]
    [InlineData(1024d, 2048d, false)]
    [InlineData(1024d, 1024.0000001, true)]
    [InlineData(null, 0.0000001, true)]
    [InlineData(null, 512d, false)]
    [InlineData(1024d, null, false)]
    [InlineData(null, null, false)]
    public void NeedsRebalance_IsTrueOnlyWhenTheGapIsTooSmallToSplit(double? previous, double? next, bool expected)
    {
        PositionCalculator.NeedsRebalance(previous, next).ShouldBe(expected);
    }

    [Fact]
    public void Between_RepeatedSplitsOfTheSameGap_NeedRebalanceBeforeCollapsing()
    {
        double previous = PositionCalculator.Step;
        double next = 2 * PositionCalculator.Step;
        var splits = 0;

        while (!PositionCalculator.NeedsRebalance(previous, next))
        {
            next = PositionCalculator.Between(previous, next);
            splits++;
        }

        // Rebalancing kicks in while the midpoint is still a distinct value.
        splits.ShouldBeInRange(25, 40);
        PositionCalculator.Between(previous, next).ShouldBeGreaterThan(previous);
    }
}
