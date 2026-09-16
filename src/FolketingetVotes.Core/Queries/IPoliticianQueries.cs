using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface IPoliticianQueries
{
    Task<PagedResult<PoliticianListItem>> SearchAsync(PoliticianFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PoliticianProfile?> GetProfileAsync(int actorId, CancellationToken cancellationToken = default);

    Task<PagedResult<PoliticianBallotRow>> GetBallotsAsync(int actorId, BallotFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);
}
