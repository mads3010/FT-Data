using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Tests;

public class BallotMathTests
{
    [Theory]
    [InlineData(10, 2, 1, BallotType.For)]
    [InlineData(2, 10, 1, BallotType.Against)]
    [InlineData(1, 2, 10, BallotType.Abstain)]
    [InlineData(1, 0, 0, BallotType.For)]
    public void Majority_picks_the_largest_present_group(int f, int a, int h, BallotType expected)
        => Assert.Equal(expected, BallotMath.Majority(f, a, h));

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(5, 5, 0)]
    [InlineData(3, 3, 3)]
    [InlineData(0, 4, 4)]
    public void Majority_is_null_when_nobody_present_or_tie(int f, int a, int h)
        => Assert.Null(BallotMath.Majority(f, a, h));

    [Fact]
    public void PartyVoteBreakdown_exposes_total_and_majority()
    {
        var row = new PartyVoteBreakdown("S", "Socialdemokratiet", 40, 1, 0, 9);
        Assert.Equal(50, row.Total);
        Assert.Equal(BallotType.For, row.Majority);
    }
}
