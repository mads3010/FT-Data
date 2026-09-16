using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Data.Queries;

/// <summary>One box, several indexes: composes the existing search services.</summary>
internal sealed class SearchQueries(IPoliticianQueries politicians, ICaseQueries cases, ITopicQueries topics, IDonorQueries donors, IQuestionQueries questions) : ISearchQueries
{
    public async Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var q = (query ?? string.Empty).Trim();
        if (q.Length < 2)
        {
            return new SearchResults(q, [], [], [], [], []);
        }

        var people = await politicians.SearchAsync(new PoliticianFilter(q), 1, 8, cancellationToken);
        var caseHits = await cases.SearchAsync(new CaseFilter(q), 1, 8, cancellationToken);
        var topicHits = await topics.SearchAsync(q, 1, 8, cancellationToken);
        var donorHits = await donors.SearchAsync(new DonorFilter(q), 1, 8, cancellationToken);
        var questionHits = await questions.SearchAsync(new QuestionFilter(q), 1, 5, cancellationToken);
        return new SearchResults(q, people.Items, caseHits.Items, topicHits.Items, donorHits.Items, questionHits.Items);
    }
}
