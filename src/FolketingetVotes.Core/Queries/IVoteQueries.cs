using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface IVoteQueries
{
    Task<PagedResult<VoteListItem>> SearchAsync(VoteFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<VoteDetail?> GetAsync(int voteId, CancellationToken cancellationToken = default);
}
