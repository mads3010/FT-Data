using FolketingetVotes.Core.Entities;
using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Core.ReadModels;

public sealed record PoliticianFilter(string? Query = null, string? PartyShortName = null, bool CurrentOnly = false);

public sealed record PoliticianListItem(
    int ActorId,
    string Name,
    string? PartyShortName,
    string? PictureUrl,
    bool IsCurrentMember,
    int BallotCount,
    double? AttendanceRate,
    int? BornYear);

public sealed record PartyMembershipRow(string PartyShortName, string PartyName, DateOnly StartDate, DateOnly? EndDate);

/// <summary>Ballot counts for one politician (overall or within one session).</summary>
public sealed record PoliticianStats(
    int Total,
    int ForCount,
    int AgainstCount,
    int AbstainCount,
    int AbsentCount,
    int WithPartyCount,
    int AgainstPartyCount,
    int MinisterTotal = 0,
    int MinisterAbsent = 0,
    int RoleTotal = 0,
    int RoleAbsent = 0)
{
    public static PoliticianStats Empty { get; } = new(0, 0, 0, 0, 0, 0, 0);

    public int Present => Total - AbsentCount;

    /// <summary>Share of votes where the member was present (cast for, against or abstain).</summary>
    public double? AttendanceRate => Total == 0 ? null : Present / (double)Total;

    /// <summary>Attendance counting only votes held while the member did not hold a ministerial post.</summary>
    public double? AttendanceRateExcludingMinisterPeriods
    {
        get
        {
            var total = Total - MinisterTotal;
            return total <= 0 ? null : (total - (AbsentCount - MinisterAbsent)) / (double)total;
        }
    }

    public bool HasMinisterPeriods => MinisterTotal > 0;

    /// <summary>Attendance counting only votes held outside ministerial posts and leave (orlov).</summary>
    public double? AttendanceRateExcludingRolePeriods
    {
        get
        {
            var total = Total - RoleTotal;
            return total <= 0 ? null : (total - (AbsentCount - RoleAbsent)) / (double)total;
        }
    }

    public bool HasRolePeriods => RoleTotal > 0;

    /// <summary>Share of present votes with a party majority where the member voted like that majority.</summary>
    public double? PartyAgreementRate
    {
        get
        {
            var decided = WithPartyCount + AgainstPartyCount;
            return decided == 0 ? null : WithPartyCount / (double)decided;
        }
    }
}

public sealed record PoliticianPeriodStatsRow(int PeriodId, string PeriodTitle, DateTime PeriodStart, PoliticianStats Stats);

public sealed record RolePeriodRow(RolePeriodKind Kind, string Title, DateOnly StartDate, DateOnly? EndDate);

/// <summary>A case in a list (search results, a member's proposals, a topic).</summary>
public sealed record CaseListItem(
    int CaseId,
    CaseType Type,
    string? Number,
    string Title,
    string? StatusName,
    int PeriodId,
    string PeriodTitle,
    int VoteCount,
    string? RoleName);

public sealed record PoliticianProfile(
    int ActorId,
    string Name,
    string? PictureUrl,
    string? CurrentPartyShortName,
    bool IsCurrentMember,
    IReadOnlyList<PartyMembershipRow> Memberships,
    PoliticianStats Overall,
    IReadOnlyList<PoliticianPeriodStatsRow> PerPeriod,
    int? BornYear,
    IReadOnlyList<RolePeriodRow> MinisterPeriods,
    IReadOnlyList<RolePeriodRow> TemporaryPeriods,
    IReadOnlyList<string> CurrentCommittees,
    IReadOnlyList<CaseListItem> Proposals,
    IReadOnlyList<RolePeriodRow> LeavePeriods,
    int QuestionsAsked,
    int QuestionsAnswered);

public sealed record PartySwitchRow(int ActorId, string Name, string FromParty, string ToParty, DateOnly Date);

/// <summary>Derives group changes from a person's merged membership spans (oldest first).</summary>
public static class PartySwitches
{
    public static IReadOnlyList<PartySwitchRow> From(int actorId, string name, IReadOnlyList<PartyMembershipRow> mergedSpans)
    {
        ArgumentNullException.ThrowIfNull(mergedSpans);
        var result = new List<PartySwitchRow>();
        for (var i = 1; i < mergedSpans.Count; i++)
        {
            var previous = mergedSpans[i - 1];
            var next = mergedSpans[i];
            if (previous.PartyShortName != next.PartyShortName)
            {
                result.Add(new PartySwitchRow(actorId, name, previous.PartyShortName, next.PartyShortName, next.StartDate));
            }
        }

        return result;
    }
}

public sealed record BallotFilter(
    int? PeriodId = null,
    BallotType? Ballot = null,
    VoteType? VoteType = null,
    bool DissentOnly = false,
    string? Query = null);

public sealed record PoliticianBallotRow(
    int VoteId,
    DateTime Date,
    string Title,
    string? CaseNumber,
    int? CaseId,
    VoteType VoteType,
    bool Passed,
    BallotType Ballot,
    string? PartyShortName,
    BallotType? PartyMajority)
{
    public bool DissentsFromParty => PartyMajority is not null && Ballot != BallotType.Absent && Ballot != PartyMajority;
}
