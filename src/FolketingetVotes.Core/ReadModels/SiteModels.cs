namespace FolketingetVotes.Core.ReadModels;

public sealed record PeriodOption(int Id, string Code, string Title, DateTime StartDate);

public sealed record SitemapEntry(string Path, DateTime? LastModified);

public sealed record MonthlyVoteCount(int Year, int Month, int Votes, int Passed)
{
    public string Label => $"{Year}-{Month:00}";
}

public sealed record SiteOverview(
    DateTime? LastSyncCompletedAt,
    int VoteCount,
    long BallotCount,
    int PoliticianCount,
    DateTime? EarliestVote,
    DateTime? LatestVote,
    IReadOnlyList<VoteListItem> LatestVotes);
