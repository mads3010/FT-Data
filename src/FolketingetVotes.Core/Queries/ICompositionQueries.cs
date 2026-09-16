using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface ICompositionQueries
{
    Task<CompositionReport> GetCurrentAsync(CancellationToken cancellationToken = default);
}
