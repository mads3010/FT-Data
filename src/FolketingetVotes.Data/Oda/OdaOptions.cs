namespace FolketingetVotes.Data.Oda;

/// <summary>Settings for talking to oda.ft.dk. Bound from the <c>Oda</c> configuration section.</summary>
public sealed class OdaOptions
{
    public const string SectionName = "Oda";

    public Uri BaseAddress { get; set; } = new("https://oda.ft.dk/api/");

    /// <summary>The server caps every page at 100 rows regardless of $top.</summary>
    public int PageSize { get; set; } = 100;

    /// <summary>Maximum simultaneous requests to the API. Keep this modest; it is a shared public service.</summary>
    public int MaxConcurrentRequests { get; set; } = 4;

    /// <summary>Id span per worker when a large entity set is loaded in parallel key ranges.</summary>
    public int RangeSize { get; set; } = 25_000;

    /// <summary>Incremental runs re-read this much history before the last checkpoint; upserts make the overlap harmless.</summary>
    public TimeSpan IncrementalOverlap { get; set; } = TimeSpan.FromMinutes(10);
}
