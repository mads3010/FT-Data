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
    double? AttendanceRate);

public sealed record PartyMembershipRow(string PartyShortName, string PartyName, DateOnly StartDate, DateOnly? EndDate);

/// <summary>Ballot counts for one politician (overall or within one session).</summary>
public sealed record PoliticianStats(
    int Total,
    int ForCount,
    int AgainstCount,
    int AbstainCount,
    int AbsentCount,
    int WithPartyCount,
    int AgainstPartyCount)
{
    public static PoliticianStats Empty { get; } = new(0, 0, 0, 0, 0, 0, 0);

    public int Present => Total - AbsentCount;

    /// <summary>Share of votes where the member was present (cast for, against or abstain).</summary>
    public double? AttendanceRate => Total == 0 ? null : Present / (double)Total;

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

public sealed record PoliticianProfile(
    int ActorId,
    string Name,
    string? PictureUrl,
    string? CurrentPartyShortName,
    bool IsCurrentMember,
    IReadOnlyList<PartyMembershipRow> Memberships,
    PoliticianStats Overall,
    IReadOnlyList<PoliticianPeriodStatsRow> PerPeriod);

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
