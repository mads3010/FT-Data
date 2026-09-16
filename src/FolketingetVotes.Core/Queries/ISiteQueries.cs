using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Core.Queries;

public interface ISiteQueries
{
    Task<SiteOverview> GetOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>Sessions that contain at least one vote, newest first.</summary>
    Task<IReadOnlyList<PeriodOption>> GetPeriodsWithVotesAsync(CancellationToken cancellationToken = default);

    /// <summary>Every public page for the sitemap.</summary>
    Task<IReadOnlyList<SitemapEntry>> GetSitemapAsync(CancellationToken cancellationToken = default);
}
