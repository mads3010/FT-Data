using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface ITopicQueries
{
    Task<PagedResult<TopicListItem>> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<TopicDetail?> GetAsync(int keywordId, int page, int pageSize, CancellationToken cancellationToken = default);
}
