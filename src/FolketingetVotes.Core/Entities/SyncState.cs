namespace FolketingetVotes.Core.Entities;

/// <summary>Bookkeeping for incremental synchronisation of one API entity set.</summary>
public sealed class SyncState
{
    public required string EntityName { get; set; }

    /// <summary>Highest <c>opdateringsdato</c> seen so far; the next incremental run starts here.</summary>
    public DateTime? LastUpdatedAt { get; set; }

    /// <summary>Highest id seen during a full (keyset) load, used to resume an interrupted backfill.</summary>
    public int? LastId { get; set; }

    public bool FullLoadCompleted { get; set; }
    public DateTime? LastRunStartedAt { get; set; }
    public DateTime? LastRunCompletedAt { get; set; }
    public long RowsUpserted { get; set; }
}
