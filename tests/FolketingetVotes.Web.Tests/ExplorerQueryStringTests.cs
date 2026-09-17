using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Web.Services;

namespace FolketingetVotes.Web.Tests;

public class ExplorerQueryStringTests
{
    [Fact]
    public void Parses_and_rebuilds_a_shareable_link()
    {
        var q = ExplorerQueryString.Parse("absence", "S,V", null, "2026-01-01", "2026-06-30", null, "month", "FinalPassage", 9, "pie");
        Assert.Equal(ExplorerMetric.Absence, q.Metric);
        Assert.Equal(["S", "V"], q.Parties);
        Assert.Equal(new DateOnly(2026, 1, 1), q.From);
        Assert.Equal(ExplorerGrouping.Month, q.Grouping);
        Assert.Equal(VoteType.FinalPassage, q.VoteType);
        Assert.Equal(9, q.KeywordId);
        Assert.Equal(ExplorerChart.Pie, q.Chart);

        var url = ExplorerQueryString.Build(q);
        Assert.StartsWith("/udforsk?metric=Absence&parties=S%2CV&from=2026-01-01&to=2026-06-30&grouping=Month&voteType=FinalPassage&keyword=9&chart=Pie", url, StringComparison.Ordinal);
    }

    [Fact]
    public void Range_presets_and_defaults()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        Assert.Equal(today.AddMonths(-6), ExplorerQueryString.Parse(null, null, null, null, null, null, null, null, null, null).From);
        Assert.Equal(new DateOnly(2004, 10, 1), ExplorerQueryString.Parse(null, null, null, null, null, "all", null, null, null, null).From);
        Assert.Equal(today.AddYears(-5), ExplorerQueryString.Parse(null, null, null, null, null, "5y", null, null, null, null).From);
        Assert.Equal(ExplorerMetric.Attendance, ExplorerQueryString.Parse("nonsense", null, null, null, null, null, null, null, null, null).Metric);
        Assert.Equal([145, 18], ExplorerQueryString.Parse(null, null, "145,18,x", null, null, null, null, null, null, null).Politicians);
    }
}
