namespace FolketingetVotes.Core.ReadModels;

public sealed record SearchResults(
    string Query,
    IReadOnlyList<PoliticianListItem> Politicians,
    IReadOnlyList<CaseListItem> Cases,
    IReadOnlyList<TopicListItem> Topics,
    IReadOnlyList<DonorListItem> Donors,
    IReadOnlyList<QuestionListItem> Questions)
{
    public int Total => Politicians.Count + Cases.Count + Topics.Count + Donors.Count + Questions.Count;
}
