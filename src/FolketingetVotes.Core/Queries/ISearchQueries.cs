using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface ISearchQueries
{
    Task<SearchResults> SearchAsync(string query, CancellationToken cancellationToken = default);
}
