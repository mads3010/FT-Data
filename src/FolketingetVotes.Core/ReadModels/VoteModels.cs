using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Core.ReadModels;

public sealed record VoteFilter(
    string? Query = null,
    int? PeriodId = null,
    VoteType? Type = null,
    bool? Passed = null,
    CaseType? CaseType = null);

public sealed record VoteListItem(
    int VoteId,
    DateTime Date,
    VoteType Type,
    bool Passed,
    int? CaseId,
    string? CaseNumber,
    string Title,
    string? StepTitle,
    int ForCount,
    int AgainstCount,
    int AbstainCount,
    int AbsentCount)
{
    public int Total => ForCount + AgainstCount + AbstainCount + AbsentCount;
}

public sealed record PartyVoteBreakdown(
    string? PartyShortName,
    string PartyName,
    int ForCount,
    int AgainstCount,
    int AbstainCount,
    int AbsentCount)
{
    public int Total => ForCount + AgainstCount + AbstainCount + AbsentCount;

    /// <summary>The ballot cast by most present members, or null when nobody was present or it is a tie.</summary>
    public BallotType? Majority => BallotMath.Majority(ForCount, AgainstCount, AbstainCount);
}

public sealed record BallotRow(
    int ActorId,
    string Name,
    string? PartyShortName,
    BallotType Ballot,
    bool DissentsFromParty);

public sealed record VoteDetail(
    VoteListItem Vote,
    string? Conclusion,
    string? Comment,
    int MeetingId,
    string MeetingTitle,
    int PeriodId,
    string PeriodTitle,
    CaseSummary? Case,
    IReadOnlyList<PartyVoteBreakdown> Parties,
    IReadOnlyList<BallotRow> Ballots);

public static class BallotMath
{
    /// <summary>Majority among present members (for / against / abstain). Null on tie or when nobody was present.</summary>
    public static BallotType? Majority(int forCount, int againstCount, int abstainCount)
    {
        var max = Math.Max(forCount, Math.Max(againstCount, abstainCount));
        if (max == 0)
        {
            return null;
        }

        var winners = (forCount == max ? 1 : 0) + (againstCount == max ? 1 : 0) + (abstainCount == max ? 1 : 0);
        if (winners > 1)
        {
            return null;
        }

        return forCount == max ? BallotType.For : againstCount == max ? BallotType.Against : BallotType.Abstain;
    }
}
