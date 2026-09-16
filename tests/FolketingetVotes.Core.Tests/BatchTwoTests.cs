using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Tests;

public class BatchTwoTests
{
    [Fact]
    public void Party_switches_are_derived_from_merged_spans()
    {
        var spans = new List<PartyMembershipRow>
        {
            new("V", "Venstre", new DateOnly(1994, 9, 21), new DateOnly(2020, 12, 31)),
            new("UFG", "Uden for", new DateOnly(2021, 1, 1), new DateOnly(2022, 5, 18)),
            new("M", "Moderaterne", new DateOnly(2022, 5, 19), null),
        };
        var switches = PartySwitches.From(145, "Lars Løkke Rasmussen", spans);
        Assert.Equal(2, switches.Count);
        Assert.Equal(("V", "UFG", new DateOnly(2021, 1, 1)), (switches[0].FromParty, switches[0].ToParty, switches[0].Date));
        Assert.Equal(("UFG", "M", new DateOnly(2022, 5, 19)), (switches[1].FromParty, switches[1].ToParty, switches[1].Date));
        Assert.Empty(PartySwitches.From(1, "x", [spans[0]]));
    }

    [Theory]
    [InlineData("20252", "S 360", "https://www.ft.dk/samling/20252/spoergsmaal/S360/index.htm")]
    [InlineData("20252", null, null)]
    public void Question_links(string code, string? number, string? expected) => Assert.Equal(expected, ExternalLinks.QuestionUrl(code, number));

    [Fact]
    public void Median_handles_even_odd_and_empty()
    {
        Assert.Equal(3, Median.Of([1, 3, 5]));
        Assert.Equal(2.5, Median.Of([1, 2, 3, 4]));
        Assert.Null(Median.Of([]));
    }

    [Fact]
    public void Attendance_excluding_role_periods_and_question_days()
    {
        var stats = new PoliticianStats(100, 10, 5, 0, 85, 15, 0, MinisterTotal: 60, MinisterAbsent: 59, RoleTotal: 80, RoleAbsent: 78);
        Assert.True(stats.HasRolePeriods);
        Assert.Equal((20 - 7) / 20.0, stats.AttendanceRateExcludingRolePeriods!.Value, 6);

        var q = new QuestionListItem(1, "S 1", "t", 1, "a", "S", "m", 2, "b", new DateTime(2024, 2, 1), new DateTime(2024, 2, 8), false, false, "2023-24", "20231");
        Assert.Equal(7, q.DaysToAnswer);
        Assert.Null((q with { AnsweredDate = null }).DaysToAnswer);
    }

    [Fact]
    public void Legislation_and_comparison_rows()
    {
        var row = new LegislationRow(Enums.CaseType.Bill, "Regeringsforslag", 232, 230);
        Assert.Equal(2, row.Rejected);
        var comparison = new PartyComparison("S", "V", "S", "V", 10, 7, new PagedResult<PartyDifferenceRow>([], 1, 10, 3));
        Assert.Equal(0.7, comparison.AgreementRate);
        Assert.Equal(0.5, new CompositionRow("S", "S", 4, 2, 2, 0, null, null).WomenShare);
    }
}
