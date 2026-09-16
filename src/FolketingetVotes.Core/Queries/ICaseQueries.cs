using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface ICaseQueries
{
    Task<CaseDetail?> GetAsync(int caseId, CancellationToken cancellationToken = default);

    Task<PagedResult<CaseListItem>> SearchAsync(CaseFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);
}
