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
    int AbsentCount,
    bool IsIndependentGroup = false)
{
    public int Total => ForCount + AgainstCount + AbstainCount + AbsentCount;

    public int Present => ForCount + AgainstCount + AbstainCount;

    /// <summary>The ballot cast by most present members; null when nobody was present, on a tie, or for independents (no group line).</summary>
    public BallotType? Majority => IsIndependentGroup || PartyShortName is null ? null : BallotMath.Majority(ForCount, AgainstCount, AbstainCount);
}

/// <summary>A plain-language description of how a vote fell, built only from the counts.</summary>
public static class VoteNarrative
{
    public static string Describe(VoteListItem vote, IReadOnlyList<PartyVoteBreakdown> parties)
    {
        ArgumentNullException.ThrowIfNull(vote);
        ArgumentNullException.ThrowIfNull(parties);
        var groups = parties.Where(p => p.PartyShortName is not null && !p.IsIndependentGroup && p.Present > 0).ToList();
        var forParties = groups.Where(p => p.Majority == BallotType.For).Select(p => p.PartyShortName!).ToList();
        var againstParties = groups.Where(p => p.Majority == BallotType.Against).Select(p => p.PartyShortName!).ToList();
        var abstainParties = groups.Where(p => p.Majority == BallotType.Abstain).Select(p => p.PartyShortName!).ToList();
        var splitParties = groups.Where(p => p.Majority is null).Select(p => p.PartyShortName!).ToList();
        var independents = parties.Where(p => p.IsIndependentGroup && p.Present > 0).ToList();

        var parts = new List<string>
        {
            $"{(vote.Passed ? "Vedtaget" : "Forkastet")} med {vote.ForCount} stemmer for og {vote.AgainstCount} imod" + (vote.AbstainCount > 0 ? $", {vote.AbstainCount} stemte hverken for eller imod" : string.Empty) + ".",
        };
        if (forParties.Count > 0)
        {
            parts.Add($"For stemte {Join(forParties)}.");
        }

        if (againstParties.Count > 0)
        {
            parts.Add($"Imod stemte {Join(againstParties)}.");
        }

        if (abstainParties.Count > 0)
        {
            parts.Add($"Hverken for eller imod: {Join(abstainParties)}.");
        }

        if (splitParties.Count > 0)
        {
            parts.Add($"Delt (lige mange for og imod): {Join(splitParties)}.");
        }

        if (independents.Count > 0)
        {
            var f = independents.Sum(p => p.ForCount);
            var a = independents.Sum(p => p.AgainstCount);
            var h = independents.Sum(p => p.AbstainCount);
            parts.Add($"Løsgængere: {f} for, {a} imod" + (h > 0 ? $", {h} hverken" : string.Empty) + ".");
        }

        parts.Add($"{vote.AbsentCount} af {vote.Total} medlemmer var fraværende.");
        return string.Join(' ', parts);
    }

    private static string Join(List<string> names) =>
        names.Count == 1 ? names[0] : string.Join(", ", names.Take(names.Count - 1)) + " og " + names[^1];
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
    string? MeetingNumber,
    int PeriodId,
    string PeriodTitle,
    string PeriodCode,
    CaseSummary? Case,
    IReadOnlyList<PartyVoteBreakdown> Parties,
    IReadOnlyList<BallotRow> Ballots)
{
    /// <summary>The verbatim transcript (referat) of the sitting on folketingstidende.dk.</summary>
    public string? TranscriptUrl => ExternalLinks.TranscriptPdfUrl(PeriodCode, MeetingNumber);
}

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
