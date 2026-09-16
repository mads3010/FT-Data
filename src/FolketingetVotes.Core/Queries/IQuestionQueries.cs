using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface IQuestionQueries
{
    Task<PagedResult<QuestionListItem>> SearchAsync(QuestionFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<QuestionStats> GetStatsAsync(QuestionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Questions per minister title in a session (or overall), with median days to answer.</summary>
    Task<IReadOnlyList<MinisterQuestionRow>> GetByMinisterAsync(int? periodId, CancellationToken cancellationToken = default);
}
