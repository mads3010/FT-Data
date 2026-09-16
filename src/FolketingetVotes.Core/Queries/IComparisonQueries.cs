using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface IComparisonQueries
{
    Task<ComparisonResult?> CompareAsync(int actorA, int actorB, int? periodId, CancellationToken cancellationToken = default);
}
