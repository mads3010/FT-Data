using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface IExplorerQueries
{
    Task<ExplorerResult> RunAsync(ExplorerQuery query, CancellationToken cancellationToken = default);

    /// <summary>Resolves a keyword name typed by the user to the best-matching keyword id and name.</summary>
    Task<(int Id, string Name)?> ResolveKeywordAsync(string name, CancellationToken cancellationToken = default);
}
