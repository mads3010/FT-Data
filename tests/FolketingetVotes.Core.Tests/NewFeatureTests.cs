using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Tests;

public class NewFeatureTests
{
    [Fact]
    public void Agreement_matrix_counts_shared_and_agreed_votes()
    {
        (int, string, int)[] rows =
        [
            (1, "S", 1), (1, "V", 1), (1, "EL", 2),
            (2, "S", 1), (2, "V", 2),
            (3, "S", 2), (3, "EL", 2),
        ];
        var result = AgreementMath.Compute(rows, ["S", "V", "EL"]);

        var sv = result.Single(a => a.PartyA == "S" && a.PartyB == "V");
        Assert.Equal((2, 1), (sv.SharedVotes, sv.AgreedVotes));
        Assert.Equal(0.5, sv.Rate);
        var sel = result.Single(a => a.PartyA == "S" && a.PartyB == "EL");
        Assert.Equal((2, 1), (sel.SharedVotes, sel.AgreedVotes));
        var vel = result.Single(a => a.PartyA == "V" && a.PartyB == "EL");
        Assert.Equal((1, 0), (vel.SharedVotes, vel.AgreedVotes));
    }

    [Theory]
    [InlineData("Dansk Industri", "dansk industri")]
    [InlineData("  DANSK  INDUSTRI ", "dansk industri")]
    [InlineData("Kjøller A/ S", "kjøller a s")]
    [InlineData("3F Bygge-Jord og Miljøarbejdernes Fagforening", "3f bygge jord og miljøarbejdernes fagforening")]
    public void Donor_names_normalise(string name, string expected) => Assert.Equal(expected, DonorNames.Normalize(name));

    [Fact]
    public void Donor_slugs_round_trip()
    {
        var key = DonorNames.Normalize("Kjøller A/ S");
        Assert.Equal(key, DonorNames.FromSlug(DonorNames.Slug(key)));
    }

    [Theory]
    [InlineData("20231", "10", "https://www.folketingstidende.dk/samling/20231/salen/M10/20231_M10_referat.pdf")]
    [InlineData("20252", "23", "https://www.folketingstidende.dk/samling/20252/salen/M23/20252_M23_referat.pdf")]
    [InlineData("20231", null, null)]
    [InlineData("20231", "x", null)]
    public void Transcript_links(string code, string? meeting, string? expected) => Assert.Equal(expected, ExternalLinks.TranscriptPdfUrl(code, meeting));

    [Fact]
    public void Attendance_excluding_minister_periods()
    {
        var stats = new PoliticianStats(Total: 100, ForCount: 10, AgainstCount: 5, AbstainCount: 0, AbsentCount: 85, WithPartyCount: 15, AgainstPartyCount: 0, MinisterTotal: 80, MinisterAbsent: 78);
        Assert.Equal(0.15, stats.AttendanceRate!.Value, 6);
        Assert.Equal((20 - 7) / 20.0, stats.AttendanceRateExcludingMinisterPeriods!.Value, 6);
        Assert.True(stats.HasMinisterPeriods);
        Assert.Null(new PoliticianStats(10, 1, 0, 0, 9, 1, 0, MinisterTotal: 10, MinisterAbsent: 9).AttendanceRateExcludingMinisterPeriods);
    }

    [Fact]
    public void Session_rates()
    {
        var s = new SessionListItem(1, "20231", "2023-24", DateTime.Today, null, VoteCount: 200, PassedCount: 150, UnanimousCount: 50);
        Assert.Equal(0.75, s.PassedRate);
        Assert.Equal(0.25, s.UnanimousRate);
        var c = new ComparisonResult(null!, null!, 10, 8, 6, []);
        Assert.Equal(0.75, c.AgreementRate);
        Assert.Equal(2, c.Differed);
        Assert.Equal(BallotType.For, BallotMath.Majority(3, 1, 0));
    }
}
