using FolketingetVotes.Data.Stats;
using Microsoft.Extensions.Logging;

namespace FolketingetVotes.Data.Sync;

/// <summary>Runs every registered <see cref="IEntitySync"/> in dependency order, then refreshes derived statistics.</summary>
public sealed class SyncOrchestrator(IEnumerable<IEntitySync> syncs, StatsRefresher stats, ILogger<SyncOrchestrator> logger)
{
    private readonly IReadOnlyList<IEntitySync> _syncs = [.. syncs.OrderBy(s => s.Order)];

    public IReadOnlyList<string> EntityNames => [.. _syncs.Select(s => s.Name)];

    public async Task<IReadOnlyList<SyncResult>> RunAsync(SyncMode mode, IReadOnlySet<string>? only = null, bool refreshStats = true, CancellationToken cancellationToken = default)
    {
        var results = new List<SyncResult>();
        foreach (var sync in _syncs)
        {
            if (only is { Count: > 0 } && !only.Contains(sync.Name))
            {
                continue;
            }

            results.Add(await sync.SyncAsync(mode, cancellationToken));
        }

        if (refreshStats)
        {
            await stats.RefreshAsync(cancellationToken);
        }

        logger.LogInformation("Sync finished: {Summary}", string.Join(", ", results.Select(r => $"{r.Name}={r.Rows}")));
        return results;
    }
}
