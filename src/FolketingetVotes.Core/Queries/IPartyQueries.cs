using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface IPartyQueries
{
    Task<IReadOnlyList<PartyListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<PartyDetail?> GetAsync(string shortName, CancellationToken cancellationToken = default);
}
