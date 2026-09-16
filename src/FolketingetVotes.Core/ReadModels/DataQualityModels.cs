namespace FolketingetVotes.Core.ReadModels;

public sealed record SyncStateRow(string EntityName, bool FullLoadCompleted, DateTime? LastRunCompletedAt, DateTime? Checkpoint, long RowsUpserted);

public sealed record BallotsPerVoteRow(int Ballots, int Votes);

public sealed record DataQualityReport(
    IReadOnlyList<SyncStateRow> Sync,
    int VoteCount,
    long BallotCount,
    int ConclusionsChecked,
    int ConclusionsMatching,
    long UnattributedBallots,
    IReadOnlyList<BallotsPerVoteRow> BallotsPerVote,
    int DonorRows,
    int DonorRowsUnreadable,
    int MembersToday,
    DateTime? LatestVote);
