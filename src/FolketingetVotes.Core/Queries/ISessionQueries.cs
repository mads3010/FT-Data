using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface ISessionQueries
{
    Task<IReadOnlyList<SessionListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<SessionDetail?> GetAsync(int periodId, CancellationToken cancellationToken = default);

    /// <summary>Ballots cast against the member's group majority in a session, newest first.</summary>
    Task<PagedResult<DissentRow>> GetDissentsAsync(int periodId, int page, int pageSize, CancellationToken cancellationToken = default);
}
