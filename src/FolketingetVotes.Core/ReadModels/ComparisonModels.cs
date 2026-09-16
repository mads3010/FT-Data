using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Core.ReadModels;

public sealed record ComparisonDifference(VoteListItem Vote, BallotType BallotA, BallotType BallotB);

public sealed record ComparisonResult(
    PoliticianListItem A,
    PoliticianListItem B,
    int SharedVotes,
    int BothPresent,
    int Agreed,
    IReadOnlyList<ComparisonDifference> LatestDifferences)
{
    public double? AgreementRate => BothPresent == 0 ? null : Agreed / (double)BothPresent;

    public int Differed => BothPresent - Agreed;
}
