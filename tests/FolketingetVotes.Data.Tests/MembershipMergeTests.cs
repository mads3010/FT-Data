using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Queries;

namespace FolketingetVotes.Data.Tests;

public class MembershipMergeTests
{
    [Fact]
    public void Merges_back_to_back_sessions_of_the_same_party()
    {
        var rows = new List<PartyMembershipRow>
        {
            new("V", "Venstre", new DateOnly(2011, 10, 4), new DateOnly(2012, 10, 2)),
            new("V", "Venstre", new DateOnly(2012, 10, 2), new DateOnly(2013, 10, 1)),
            new("V", "Venstre", new DateOnly(2013, 10, 1), new DateOnly(2015, 6, 18)),
            new("UFG", "Uden for folketingsgrupperne", new DateOnly(2021, 1, 1), new DateOnly(2022, 5, 18)),
            new("M", "Moderaterne", new DateOnly(2022, 5, 19), null),
            new("M", "Moderaterne", new DateOnly(2022, 10, 4), null),
        };

        var merged = PoliticianQueries.MergeConsecutive(rows);

        Assert.Equal(3, merged.Count);
        Assert.Equal(("V", new DateOnly(2011, 10, 4), new DateOnly(2015, 6, 18)), (merged[0].PartyShortName, merged[0].StartDate, merged[0].EndDate));
        Assert.Equal("UFG", merged[1].PartyShortName);
        Assert.Equal(("M", new DateOnly(2022, 5, 19), (DateOnly?)null), (merged[2].PartyShortName, merged[2].StartDate, merged[2].EndDate));
    }

    [Fact]
    public void Keeps_a_gap_as_two_spans()
    {
        var rows = new List<PartyMembershipRow>
        {
            new("S", "Socialdemokratiet", new DateOnly(2007, 11, 13), new DateOnly(2011, 9, 15)),
            new("S", "Socialdemokratiet", new DateOnly(2015, 6, 18), null),
        };

        var merged = PoliticianQueries.MergeConsecutive(rows);
        Assert.Equal(2, merged.Count);
    }
}
