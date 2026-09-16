using System.Diagnostics;
using FolketingetVotes.Core.Entities;
using FolketingetVotes.Data.Oda;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FolketingetVotes.Data.Sync;

/// <summary>
/// Generic sync for one entity set: a full load walks ids in ascending order (optionally in parallel
/// ranges); an incremental load reads everything updated since the last checkpoint. Both upsert.
/// </summary>
public abstract class EntitySync<TDto, TEntity> : IEntitySync
    where TDto : IOdaRecord
    where TEntity : class
{
    private readonly OdaClient _oda;
    private readonly IDbContextFactory<FolketingetDbContext> _dbFactory;
    private readonly OdaOptions _options;
    private readonly ILogger _logger;

    protected EntitySync(OdaClient oda, IDbContextFactory<FolketingetDbContext> dbFactory, IOptions<OdaOptions> options, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _oda = oda;
        _dbFactory = dbFactory;
        _options = options.Value;
        _logger = logger;
    }

    public abstract string Name { get; }

    public abstract int Order { get; }

    /// <summary>Rows per database round-trip.</summary>
    protected virtual int BatchSize => 1000;

    /// <summary>Large sets are split into id ranges fetched concurrently (bounded by <see cref="OdaOptions.MaxConcurrentRequests"/>).</summary>
    protected virtual bool LoadInParallelRanges => false;

    protected abstract TEntity Map(TDto dto);

    protected abstract Task UpsertAsync(FolketingetDbContext db, IReadOnlyList<TEntity> batch, CancellationToken cancellationToken);

    public async Task<SyncResult> SyncAsync(SyncMode mode, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var stateDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var state = await stateDb.SyncStates.FindAsync([Name], cancellationToken);
        if (state is null)
        {
            state = new SyncState { EntityName = Name };
            stateDb.SyncStates.Add(state);
        }

        state.LastRunStartedAt = DateTime.UtcNow;
        await stateDb.SaveChangesAsync(cancellationToken);

        var full = mode == SyncMode.Full || !state.FullLoadCompleted;
        _logger.LogInformation("{Entity}: starting {Mode} load", Name, full ? "full" : "incremental");

        var rows = full
            ? await FullLoadAsync(stateDb, state, cancellationToken)
            : await IncrementalLoadAsync(stateDb, state, cancellationToken);

        state.LastRunCompletedAt = DateTime.UtcNow;
        state.RowsUpserted += rows;
        await stateDb.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{Entity}: upserted {Rows} rows in {Elapsed}", Name, rows, stopwatch.Elapsed);
        return new SyncResult(Name, rows, stopwatch.Elapsed);
    }

    private async Task<long> FullLoadAsync(FolketingetDbContext stateDb, SyncState state, CancellationToken cancellationToken)
    {
        var maxId = await _oda.MaxIdAsync(Name, cancellationToken);
        var startId = state.FullLoadCompleted ? 0 : state.LastId ?? 0;
        var tracker = new Tracker(state.LastUpdatedAt);
        long rows = 0;

        if (LoadInParallelRanges && maxId - startId > _options.RangeSize)
        {
            var ranges = new List<(int From, int To)>();
            for (var from = startId; from < maxId; from += _options.RangeSize)
            {
                ranges.Add((from, Math.Min(from + _options.RangeSize, maxId)));
            }

            _logger.LogInformation("{Entity}: {Ranges} id ranges up to id {MaxId}", Name, ranges.Count, maxId);
            var parallelism = new ParallelOptions { MaxDegreeOfParallelism = _options.MaxConcurrentRequests, CancellationToken = cancellationToken };
            await Parallel.ForEachAsync(ranges, parallelism, async (range, token) =>
            {
                await using var workDb = await _dbFactory.CreateDbContextAsync(token);
                var source = _oda.EnumerateByIdAsync<TDto>(Name, range.From, range.To, token);
                var count = await ConsumeAsync(workDb, source, tracker, token);
                Interlocked.Add(ref rows, count);
                _logger.LogInformation("{Entity}: range ({From}, {To}] done, {Rows} rows so far", Name, range.From, range.To, Interlocked.Read(ref rows));
            });
        }
        else
        {
            await using var workDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var source = _oda.EnumerateByIdAsync<TDto>(Name, startId, null, cancellationToken);
            rows = await ConsumeAsync(workDb, source, tracker, cancellationToken, async lastId =>
            {
                state.LastId = lastId;
                state.LastUpdatedAt = tracker.MaxUpdatedAt;
                await stateDb.SaveChangesAsync(cancellationToken);
            });
        }

        state.LastId = maxId;
        state.LastUpdatedAt = tracker.MaxUpdatedAt;
        state.FullLoadCompleted = true;
        return rows;
    }

    private async Task<long> IncrementalLoadAsync(FolketingetDbContext stateDb, SyncState state, CancellationToken cancellationToken)
    {
        var since = (state.LastUpdatedAt ?? DateTime.MinValue) - _options.IncrementalOverlap;
        var filter = $"opdateringsdato gt {OdaClient.FormatDateTime(since)}";
        var tracker = new Tracker(state.LastUpdatedAt);

        await using var workDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var source = _oda.EnumerateAsync<TDto>(Name, filter, "opdateringsdato,id", cancellationToken);
        var rows = await ConsumeAsync(workDb, source, tracker, cancellationToken, async _ =>
        {
            state.LastUpdatedAt = tracker.MaxUpdatedAt;
            await stateDb.SaveChangesAsync(cancellationToken);
        });

        state.LastUpdatedAt = tracker.MaxUpdatedAt;
        return rows;
    }

    private async Task<long> ConsumeAsync(
        FolketingetDbContext workDb,
        IAsyncEnumerable<TDto> source,
        Tracker tracker,
        CancellationToken cancellationToken,
        Func<int, Task>? afterBatch = null)
    {
        long rows = 0;
        var batch = new List<TEntity>(BatchSize);
        var lastId = 0;
        await foreach (var dto in source.WithCancellation(cancellationToken))
        {
            batch.Add(Map(dto));
            tracker.Observe(dto.UpdatedAt);
            lastId = dto.Id;
            if (batch.Count >= BatchSize)
            {
                rows += await FlushAsync(workDb, batch, lastId, afterBatch, cancellationToken);
            }
        }

        if (batch.Count > 0)
        {
            rows += await FlushAsync(workDb, batch, lastId, afterBatch, cancellationToken);
        }

        return rows;
    }

    private async Task<int> FlushAsync(FolketingetDbContext workDb, List<TEntity> batch, int lastId, Func<int, Task>? afterBatch, CancellationToken cancellationToken)
    {
        await UpsertAsync(workDb, batch, cancellationToken);
        var count = batch.Count;
        batch.Clear();
        if (afterBatch is not null)
        {
            await afterBatch(lastId);
        }

        return count;
    }

    /// <summary>Thread-safe high-water mark of <c>opdateringsdato</c>.</summary>
    private sealed class Tracker(DateTime? initial)
    {
        private long _ticks = (initial ?? DateTime.MinValue).Ticks;

        public DateTime MaxUpdatedAt => new(Interlocked.Read(ref _ticks));

        public void Observe(DateTime value)
        {
            long current;
            while ((current = Interlocked.Read(ref _ticks)) < value.Ticks
                   && Interlocked.CompareExchange(ref _ticks, value.Ticks, current) != current)
            {
            }
        }
    }
}
