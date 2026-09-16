namespace FolketingetVotes.Data.Sync;

public enum SyncMode
{
    /// <summary>Fetch only rows whose <c>opdateringsdato</c> is newer than the last checkpoint.</summary>
    Incremental,

    /// <summary>Walk the whole entity set by id. Also used automatically until the first full load completes.</summary>
    Full,
}

public sealed record SyncResult(string Name, long Rows, TimeSpan Elapsed);

/// <summary>Synchronises one oda.ft.dk entity set into the database.</summary>
public interface IEntitySync
{
    /// <summary>The API entity set name, e.g. "Afstemning". Also the key in <c>sync_states</c>.</summary>
    string Name { get; }

    /// <summary>Execution order; parents before children so foreign references resolve.</summary>
    int Order { get; }

    Task<SyncResult> SyncAsync(SyncMode mode, CancellationToken cancellationToken = default);
}
