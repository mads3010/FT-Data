using System.Diagnostics;
using System.Reflection;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FolketingetVotes.Data.Stats;

/// <summary>
/// Rebuilds the derived tables (parties, party memberships) and the <c>mv_*</c> materialized views
/// from the embedded SQL scripts in <c>Sql/</c>. Safe to run at any time; runs after every sync.
/// </summary>
public sealed class StatsRefresher(IDbContextFactory<FolketingetDbContext> dbFactory, ILogger<StatsRefresher> logger)
{
    private static readonly string[] Scripts =
    [
        "010_parties.sql",
        "020_party_memberships.sql",
        "030_materialized_views.sql",
    ];

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(60));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var script in Scripts)
        {
            var sql = await ReadScriptAsync(script, cancellationToken);
            logger.LogInformation("Stats: running {Script}", script);
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Stats: refreshed in {Elapsed}", stopwatch.Elapsed);
    }

    private static async Task<string> ReadScriptAsync(string name, CancellationToken cancellationToken)
    {
        var resource = $"FolketingetVotes.Data.Sql.{name}";
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded SQL script '{resource}' not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
