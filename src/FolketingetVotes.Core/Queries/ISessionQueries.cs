using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface ISessionQueries
{
    Task<IReadOnlyList<SessionListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<SessionDetail?> GetAsync(int periodId, CancellationToken cancellationToken = default);
}
