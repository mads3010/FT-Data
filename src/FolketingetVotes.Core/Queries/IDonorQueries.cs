using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface IDonorQueries
{
    Task<PagedResult<DonorListItem>> SearchAsync(DonorFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<DonorDetail?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetYearsAsync(CancellationToken cancellationToken = default);
}
