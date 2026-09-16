namespace FolketingetVotes.Core.ReadModels;

public sealed record SessionListItem(
    int PeriodId,
    string Code,
    string Title,
    DateTime StartDate,
    DateTime? EndDate,
    int VoteCount,
    int PassedCount,
    int UnanimousCount)
{
    public double? PassedRate => VoteCount == 0 ? null : PassedCount / (double)VoteCount;

    public double? UnanimousRate => VoteCount == 0 ? null : UnanimousCount / (double)VoteCount;
}

public sealed record SessionPartyRow(string ShortName, string Name, int Members, int Ballots, int PresentBallots, int WithMajority, int AgainstMajority)
{
    public double? AttendanceRate => Ballots == 0 ? null : PresentBallots / (double)Ballots;

    public double? CohesionRate => WithMajority + AgainstMajority == 0 ? null : WithMajority / (double)(WithMajority + AgainstMajority);
}

/// <summary>How often two parties' majorities coincided in votes where both had a majority.</summary>
public sealed record PartyAgreement(string PartyA, string PartyB, int SharedVotes, int AgreedVotes)
{
    public double? Rate => SharedVotes == 0 ? null : AgreedVotes / (double)SharedVotes;
}

/// <summary>Bills or resolutions of one category in a session and how they fared at the final vote.</summary>
public sealed record LegislationRow(Enums.CaseType Type, string Category, int VotedOn, int Passed)
{
    public int Rejected => VotedOn - Passed;
}

public sealed record DissentRow(int ActorId, string Name, string? PartyShortName, VoteListItem Vote, Enums.BallotType Ballot, Enums.BallotType Majority);

public sealed record SessionDetail(
    SessionListItem Session,
    IReadOnlyList<SessionPartyRow> Parties,
    IReadOnlyList<VoteListItem> ClosestVotes,
    IReadOnlyList<string> AgreementParties,
    IReadOnlyList<PartyAgreement> Agreements,
    IReadOnlyList<LegislationRow> Legislation,
    double? MedianDaysFromIntroductionToFinalVote,
    int DissentCount)
{
    public PartyAgreement? Agreement(string a, string b) =>
        Agreements.FirstOrDefault(x => (x.PartyA == a && x.PartyB == b) || (x.PartyA == b && x.PartyB == a));
}

/// <summary>Pure computation of the agreement matrix from (vote, party, majority) rows.</summary>
public static class AgreementMath
{
    public static IReadOnlyList<PartyAgreement> Compute(IEnumerable<(int VoteId, string Party, int Majority)> rows, IReadOnlyList<string> parties)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(parties);
        var byVote = rows.GroupBy(r => r.VoteId).Select(g => g.ToDictionary(r => r.Party, r => r.Majority)).ToList();
        var result = new List<PartyAgreement>();
        for (var i = 0; i < parties.Count; i++)
        {
            for (var j = i + 1; j < parties.Count; j++)
            {
                var shared = 0;
                var agreed = 0;
                foreach (var vote in byVote)
                {
                    if (vote.TryGetValue(parties[i], out var a) && vote.TryGetValue(parties[j], out var b))
                    {
                        shared++;
                        if (a == b)
                        {
                            agreed++;
                        }
                    }
                }

                result.Add(new PartyAgreement(parties[i], parties[j], shared, agreed));
            }
        }

        return result;
    }
}
