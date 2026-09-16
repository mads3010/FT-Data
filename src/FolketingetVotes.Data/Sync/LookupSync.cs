using System.Diagnostics;
using FolketingetVotes.Core.Entities;
using FolketingetVotes.Core.Enums;
using FolketingetVotes.Data.Oda;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FolketingetVotes.Data.Sync;

/// <summary>Loads the API's small code tables into the single <c>lookups</c> table. Always a full load; they are tiny.</summary>
public sealed class LookupSync(OdaClient oda, IDbContextFactory<FolketingetDbContext> dbFactory, ILogger<LookupSync> logger) : IEntitySync
{
    public static readonly IReadOnlyDictionary<LookupKind, string> EntitySets = new Dictionary<LookupKind, string>
    {
        [LookupKind.ActorType] = "Aktørtype",
        [LookupKind.ActorRelationRole] = "AktørAktørRolle",
        [LookupKind.CaseType] = "Sagstype",
        [LookupKind.CaseStatus] = "Sagsstatus",
        [LookupKind.CaseCategory] = "Sagskategori",
        [LookupKind.CaseStepType] = "Sagstrinstype",
        [LookupKind.CaseStepStatus] = "Sagstrinsstatus",
        [LookupKind.CaseActorRole] = "SagAktørRolle",
        [LookupKind.VoteType] = "Afstemningstype",
        [LookupKind.BallotType] = "Stemmetype",
        [LookupKind.MeetingType] = "Mødetype",
        [LookupKind.MeetingStatus] = "Mødestatus",
    };

    public string Name => "Lookups";

    public int Order => 0;

    public async Task<SyncResult> SyncAsync(SyncMode mode, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.Lookups.ToDictionaryAsync(l => (l.Kind, l.Id), cancellationToken);
        long rows = 0;

        foreach (var (kind, entitySet) in EntitySets)
        {
            await foreach (var dto in oda.EnumerateAsync<OdaLookup>(entitySet, null, "id", cancellationToken))
            {
                if (existing.TryGetValue((kind, dto.Id), out var current))
                {
                    current.Name = dto.Name;
                    current.UpdatedAt = dto.UpdatedAt;
                }
                else
                {
                    db.Lookups.Add(new Lookup { Kind = kind, Id = dto.Id, Name = dto.Name, UpdatedAt = dto.UpdatedAt });
                }

                rows++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        var state = await db.SyncStates.FindAsync([Name], cancellationToken) ?? db.SyncStates.Add(new SyncState { EntityName = Name }).Entity;
        state.FullLoadCompleted = true;
        state.LastRunStartedAt ??= DateTime.UtcNow;
        state.LastRunCompletedAt = DateTime.UtcNow;
        state.RowsUpserted += rows;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Lookups: {Rows} rows in {Elapsed}", rows, stopwatch.Elapsed);
        return new SyncResult(Name, rows, stopwatch.Elapsed);
    }
}
