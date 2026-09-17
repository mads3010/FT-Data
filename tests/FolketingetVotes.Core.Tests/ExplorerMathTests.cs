using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Tests;

public class ExplorerMathTests
{
    private static readonly ExplorerMath.Counts Sample = new(Total: 100, Absent: 30, For: 50, Against: 15, Abstain: 5, WithMajority: 60, AgainstMajority: 10, Votes: 40, Dissents: 10, Questions: 3);

    [Theory]
    [InlineData(ExplorerMetric.Attendance, 0.7, 100)]
    [InlineData(ExplorerMetric.Absence, 0.3, 100)]
    [InlineData(ExplorerMetric.ForShare, 50 / 70.0, 70)]
    [InlineData(ExplorerMetric.AgainstShare, 15 / 70.0, 70)]
    [InlineData(ExplorerMetric.AbstainShare, 5 / 70.0, 70)]
    [InlineData(ExplorerMetric.Cohesion, 60 / 70.0, 70)]
    [InlineData(ExplorerMetric.Ballots, 100, 100)]
    [InlineData(ExplorerMetric.Votes, 40, 40)]
    [InlineData(ExplorerMetric.Dissents, 10, 70)]
    [InlineData(ExplorerMetric.Questions, 3, 3)]
    public void Metrics_are_computed_from_counts(ExplorerMetric metric, double expected, int basis)
    {
        var (value, b) = ExplorerMath.Evaluate(metric, Sample);
        Assert.Equal(expected, value!.Value, 9);
        Assert.Equal(basis, b);
    }

    [Fact]
    public void Shares_are_null_without_a_basis()
    {
        var empty = new ExplorerMath.Counts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        Assert.Null(ExplorerMath.Evaluate(ExplorerMetric.Attendance, empty).Value);
        Assert.Null(ExplorerMath.Evaluate(ExplorerMetric.Cohesion, empty).Value);
        Assert.Equal(0, ExplorerMath.Evaluate(ExplorerMetric.Ballots, empty).Value);
    }

    [Fact]
    public void Buckets_label_and_sort()
    {
        var d = new DateOnly(2026, 5, 17);
        Assert.Equal(("2026-05", "2026-05"), ExplorerMath.Bucket(ExplorerGrouping.Month, d, "2025-26", new DateOnly(2025, 10, 7)));
        Assert.Equal(("2026 K2", "2026-2"), ExplorerMath.Bucket(ExplorerGrouping.Quarter, d, "x", d));
        Assert.Equal(("2026", "2026"), ExplorerMath.Bucket(ExplorerGrouping.Year, d, "x", d));
        Assert.Equal(("2025-26", "2025-10-07"), ExplorerMath.Bucket(ExplorerGrouping.Session, d, "2025-26", new DateOnly(2025, 10, 7)));
        Assert.Equal(("I alt", "0"), ExplorerMath.Bucket(ExplorerGrouping.None, d, "x", d));
    }

    [Fact]
    public void Query_flags()
    {
        var q = new ExplorerQuery(ExplorerMetric.Cohesion, ["S"], [], null, null, ExplorerGrouping.None, null, null, ExplorerChart.Bar);
        Assert.True(q.IsPercent);
        Assert.False(q.ByPoliticians);
        Assert.False((q with { Metric = ExplorerMetric.Votes }).IsPercent);
        Assert.True((q with { Politicians = [1, 2] }).ByPoliticians);
    }
}
