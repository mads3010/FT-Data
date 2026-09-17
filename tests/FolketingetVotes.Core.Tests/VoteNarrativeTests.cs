using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Tests;

public class VoteNarrativeTests
{
    private static readonly VoteListItem Vote = new(1, new DateTime(2026, 9, 3), VoteType.FinalPassage, true, 1, "B 16", "t", "2. behandling", 106, 6, 0, 67);

    [Fact]
    public void Describes_outcome_party_lines_independents_and_absence()
    {
        var parties = new List<PartyVoteBreakdown>
        {
            new("S", "Socialdemokratiet", 17, 0, 0, 15),
            new("V", "Venstre", 10, 0, 0, 5),
            new("DD", "Danmarksdemokraterne", 0, 5, 0, 3),
            new("ALT", "Alternativet", 1, 1, 0, 1),
            new("UFG", "Uden for", 3, 1, 0, 0, IsIndependentGroup: true),
            new(null, "Ukendt", 2, 0, 0, 1),
            new("IA", "Inuit Ataqatigiit", 0, 0, 0, 1),
        };

        var text = VoteNarrative.Describe(Vote, parties);

        Assert.Equal("Vedtaget med 106 stemmer for og 6 imod. For stemte S og V. Imod stemte DD. Delt (lige mange for og imod): ALT. Løsgængere: 3 for, 1 imod. 67 af 179 medlemmer var fraværende.", text);
    }

    [Fact]
    public void Independents_and_unattributed_never_get_a_majority()
    {
        Assert.Null(new PartyVoteBreakdown("UFG", "x", 5, 0, 0, 0, IsIndependentGroup: true).Majority);
        Assert.Null(new PartyVoteBreakdown(null, "x", 5, 0, 0, 0).Majority);
        Assert.Equal(BallotType.For, new PartyVoteBreakdown("S", "x", 5, 1, 0, 0).Majority);
    }

    [Fact]
    public void Rejected_vote_with_abstentions_and_three_parties()
    {
        var vote = Vote with { Passed = false, ForCount = 40, AgainstCount = 60, AbstainCount = 4 };
        var text = VoteNarrative.Describe(vote, [new("A", "a", 1, 0, 0, 0), new("B", "b", 1, 0, 0, 0), new("C", "c", 0, 1, 0, 0)]);
        Assert.StartsWith("Forkastet med 40 stemmer for og 60 imod, 4 stemte hverken for eller imod. For stemte A og B. Imod stemte C.", text, StringComparison.Ordinal);
    }
}
