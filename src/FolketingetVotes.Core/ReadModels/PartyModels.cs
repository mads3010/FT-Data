namespace FolketingetVotes.Core.ReadModels;

public sealed record PartyListItem(string ShortName, string Name, int CurrentMembers, DateOnly? FirstSeen, DateOnly? LastSeen);

public sealed record PartyMemberRow(int ActorId, string Name, string? PictureUrl, DateOnly Since);

public sealed record PartyPeriodStatsRow(
    int PeriodId,
    string PeriodTitle,
    DateTime PeriodStart,
    int Members,
    int Ballots,
    int PresentBallots,
    int WithMajority,
    int AgainstMajority)
{
    public double? AttendanceRate => Ballots == 0 ? null : PresentBallots / (double)Ballots;

    /// <summary>Share of present ballots (in votes with a party majority) that matched the party majority.</summary>
    public double? CohesionRate
    {
        get
        {
            var decided = WithMajority + AgainstMajority;
            return decided == 0 ? null : WithMajority / (double)decided;
        }
    }
}

public sealed record DonationRow(string DonorName, string? DonorAddress, decimal? Amount, string? Note, int? SourcePage, string RawText);

public sealed record PartyAccountRow(int Year, string SourceFile, IReadOnlyList<DonationRow> Donations);

public sealed record PartyDetail(
    string ShortName,
    string Name,
    IReadOnlyList<PartyMemberRow> CurrentMembers,
    IReadOnlyList<PartyPeriodStatsRow> PerPeriod,
    IReadOnlyList<PartyAccountRow> Accounts);
