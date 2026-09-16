using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Tests;

public class PoliticianStatsTests
{
    [Fact]
    public void Attendance_excludes_absences_only()
    {
        var stats = new PoliticianStats(Total: 100, ForCount: 60, AgainstCount: 20, AbstainCount: 5, AbsentCount: 15, WithPartyCount: 80, AgainstPartyCount: 5);
        Assert.Equal(85, stats.Present);
        Assert.Equal(0.85, stats.AttendanceRate!.Value, 6);
    }

    [Fact]
    public void Party_agreement_ignores_votes_without_a_party_majority()
    {
        var stats = new PoliticianStats(100, 60, 20, 5, 15, WithPartyCount: 30, AgainstPartyCount: 10);
        Assert.Equal(0.75, stats.PartyAgreementRate!.Value, 6);
    }

    [Fact]
    public void Rates_are_null_without_data()
    {
        Assert.Null(PoliticianStats.Empty.AttendanceRate);
        Assert.Null(PoliticianStats.Empty.PartyAgreementRate);
    }

    [Fact]
    public void Ballot_row_flags_dissent_only_when_present_and_majority_known()
    {
        var baseRow = new PoliticianBallotRow(1, DateTime.Today, "t", "L 1", 1, VoteType.FinalPassage, true, BallotType.Against, "S", BallotType.For);
        Assert.True(baseRow.DissentsFromParty);
        Assert.False((baseRow with { Ballot = BallotType.Absent }).DissentsFromParty);
        Assert.False((baseRow with { PartyMajority = null }).DissentsFromParty);
        Assert.False((baseRow with { Ballot = BallotType.For }).DissentsFromParty);
    }
}
