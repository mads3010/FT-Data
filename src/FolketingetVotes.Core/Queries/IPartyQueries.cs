using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface IPartyQueries
{
    Task<IReadOnlyList<PartyListItem>> ListAsync(CancellationToken cancellationToken = default);

    Task<PartyDetail?> GetAsync(string shortName, CancellationToken cancellationToken = default);

    /// <summary>Every change of parliamentary group, newest first.</summary>
    Task<IReadOnlyList<PartySwitchRow>> GetSwitchesAsync(CancellationToken cancellationToken = default);

    /// <summary>Votes where two groups' majorities differed.</summary>
    Task<PartyComparison?> CompareAsync(string partyA, string partyB, int? periodId, int page, int pageSize, CancellationToken cancellationToken = default);
}
