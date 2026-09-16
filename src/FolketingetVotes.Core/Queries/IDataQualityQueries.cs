using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface IDataQualityQueries
{
    Task<DataQualityReport> GetAsync(CancellationToken cancellationToken = default);
}
