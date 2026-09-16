using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FolketingetVotes.Data.Oda;

/// <summary>
/// Minimal OData v3 client for oda.ft.dk. Pages are capped at 100 rows by the server; this client follows
/// <c>odata.nextLink</c> for small result sets and uses id-keyset paging for large ones.
/// </summary>
public sealed class OdaClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly OdaOptions _options;
    private readonly ILogger<OdaClient> _logger;
    private readonly SemaphoreSlim _throttle;

    public OdaClient(HttpClient http, IOptions<OdaOptions> options, ILogger<OdaClient> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _http = http;
        _options = options.Value;
        _logger = logger;
        _throttle = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrentRequests));
        _http.BaseAddress ??= _options.BaseAddress;
    }

    /// <summary>Total rows in an entity set, optionally filtered.</summary>
    public async Task<int> CountAsync(string entitySet, string? filter = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(entitySet, filter, orderBy: null, top: 1, inlineCount: true);
        var page = await GetPageAsync<JsonElement>(url, cancellationToken);
        return page.TotalCount ?? 0;
    }

    /// <summary>The highest id currently in an entity set (0 when empty).</summary>
    public async Task<int> MaxIdAsync(string entitySet, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(entitySet, filter: null, orderBy: "id desc", top: 1, inlineCount: false);
        var page = await GetPageAsync<JsonElement>(url, cancellationToken);
        return page.Value.Count == 0 ? 0 : page.Value[0].GetProperty("id").GetInt32();
    }

    /// <summary>Enumerates every row matching a filter by following the server's next links (uses $skip; fine for modest result sets).</summary>
    public async IAsyncEnumerable<T> EnumerateAsync<T>(
        string entitySet,
        string? filter,
        string orderBy,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(entitySet, filter, orderBy, _options.PageSize, inlineCount: false);
        while (url is not null)
        {
            var page = await GetPageAsync<T>(url, cancellationToken);
            foreach (var item in page.Value)
            {
                yield return item;
            }

            url = page.NextLink;
        }
    }

    /// <summary>
    /// Enumerates rows with <c>id</c> in (fromExclusive, toInclusive] in ascending id order, re-issuing the
    /// filter from the last id seen. Cheap for the server no matter how deep into the set we are.
    /// </summary>
    public async IAsyncEnumerable<T> EnumerateByIdAsync<T>(
        string entitySet,
        int fromExclusive,
        int? toInclusive,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : IOdaRecord
    {
        var lastId = fromExclusive;
        while (true)
        {
            var filter = toInclusive is { } to
                ? $"id gt {lastId} and id le {to}"
                : $"id gt {lastId}";
            var url = BuildUrl(entitySet, filter, "id", _options.PageSize, inlineCount: false);
            var page = await GetPageAsync<T>(url, cancellationToken);
            if (page.Value.Count == 0)
            {
                yield break;
            }

            foreach (var item in page.Value)
            {
                yield return item;
            }

            lastId = page.Value[^1].Id;
        }
    }

    public void Dispose() => _throttle.Dispose();

    public static string FormatDateTime(DateTime value) =>
        "datetime'" + value.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture) + "'";

    private static string BuildUrl(string entitySet, string? filter, string? orderBy, int top, bool inlineCount)
    {
        var parts = new List<string>(6);
        if (!string.IsNullOrEmpty(filter))
        {
            parts.Add("%24filter=" + Uri.EscapeDataString(filter));
        }

        if (!string.IsNullOrEmpty(orderBy))
        {
            parts.Add("%24orderby=" + Uri.EscapeDataString(orderBy));
        }

        parts.Add("%24top=" + top.ToString(CultureInfo.InvariantCulture));
        if (inlineCount)
        {
            parts.Add("%24inlinecount=allpages");
        }

        parts.Add("%24format=json");
        return Uri.EscapeDataString(entitySet) + "?" + string.Join('&', parts);
    }

    private async Task<OdaPage<T>> GetPageAsync<T>(string url, CancellationToken cancellationToken)
    {
        await _throttle.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("GET {Url}", url);
            var page = await _http.GetFromJsonAsync<OdaPage<T>>(url, JsonOptions, cancellationToken);
            return page ?? throw new InvalidOperationException($"Empty response from {url}");
        }
        finally
        {
            _throttle.Release();
        }
    }
}
