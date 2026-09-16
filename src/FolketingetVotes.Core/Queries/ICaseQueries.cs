using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface ICaseQueries
{
    Task<CaseDetail?> GetAsync(int caseId, CancellationToken cancellationToken = default);
}
