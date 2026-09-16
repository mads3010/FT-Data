namespace FolketingetVotes.Core.ReadModels;

public sealed record TopicListItem(int KeywordId, string Name, int TypeId, int CaseCount, int VoteCount);

/// <summary>How a party's majority fell in the final-passage votes on cases carrying a keyword.</summary>
public sealed record PartyTopicPosition(string PartyShortName, string PartyName, int VotesFor, int VotesAgainst, int VotesAbstain)
{
    public int Total => VotesFor + VotesAgainst + VotesAbstain;
}

public sealed record TopicSessionRow(int PeriodId, string PeriodTitle, int Votes, int FinalVotes, int Passed);

public sealed record TopicDetail(
    int KeywordId,
    string Name,
    int TypeId,
    int CaseCount,
    IReadOnlyList<PartyTopicPosition> Parties,
    PagedResult<VoteListItem> Votes,
    IReadOnlyList<TopicSessionRow> PerSession,
    int? PeriodId);
