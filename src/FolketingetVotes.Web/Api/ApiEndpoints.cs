using System.Globalization;
using System.Text;
using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using Microsoft.AspNetCore.Mvc;

namespace FolketingetVotes.Web.Api;

/// <summary>
/// A small read-only JSON API over the same query services the pages use, so the data stays open
/// for other tools. Politician ballots can also be exported as CSV.
/// </summary>
public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapFolketingetApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");

        api.MapGet("/status", async (ISiteQueries site, CancellationToken ct) => Results.Ok(await site.GetOverviewAsync(ct)));
        api.MapGet("/periods", async (ISiteQueries site, CancellationToken ct) => Results.Ok(await site.GetPeriodsWithVotesAsync(ct)));

        api.MapGet("/votes", async (
            IVoteQueries votes,
            [FromQuery(Name = "q")] string? query,
            [FromQuery] int? period,
            [FromQuery] VoteType? type,
            [FromQuery] bool? passed,
            [FromQuery] CaseType? caseType,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50) =>
        {
            var result = await votes.SearchAsync(new VoteFilter(query, period, type, passed, caseType), Clamp(page), ClampSize(pageSize), ct);
            return Results.Ok(result);
        });

        api.MapGet("/votes/{id:int}", async (int id, IVoteQueries votes, CancellationToken ct) =>
            await votes.GetAsync(id, ct) is { } vote ? Results.Ok(vote) : Results.NotFound());

        api.MapGet("/politicians", async (
            IPoliticianQueries politicians,
            [FromQuery(Name = "q")] string? query,
            [FromQuery] string? party,
            CancellationToken ct,
            [FromQuery] bool current = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50) =>
            Results.Ok(await politicians.SearchAsync(new PoliticianFilter(query, party, current), Clamp(page), ClampSize(pageSize), ct)));

        api.MapGet("/politicians/{id:int}", async (int id, IPoliticianQueries politicians, CancellationToken ct) =>
            await politicians.GetProfileAsync(id, ct) is { } profile ? Results.Ok(profile) : Results.NotFound());

        api.MapGet("/politicians/{id:int}/ballots", async (
            int id,
            IPoliticianQueries politicians,
            [FromQuery] int? period,
            [FromQuery] BallotType? ballot,
            [FromQuery] VoteType? voteType,
            [FromQuery(Name = "q")] string? query,
            [FromQuery] string? format,
            CancellationToken ct,
            [FromQuery] bool dissentOnly = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50) =>
        {
            var filter = new BallotFilter(period, ballot, voteType, dissentOnly, query);
            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var all = await politicians.GetBallotsAsync(id, filter, 1, 100_000, ct);
                return Results.Text(ToCsv(all.Items), "text/csv", Encoding.UTF8);
            }

            return Results.Ok(await politicians.GetBallotsAsync(id, filter, Clamp(page), ClampSize(pageSize), ct));
        });

        api.MapGet("/parties", async (IPartyQueries parties, CancellationToken ct) => Results.Ok(await parties.ListAsync(ct)));
        api.MapGet("/parties/{shortName}", async (string shortName, IPartyQueries parties, CancellationToken ct) =>
            await parties.GetAsync(shortName, ct) is { } party ? Results.Ok(party) : Results.NotFound());

        api.MapGet("/cases/{id:int}", async (int id, ICaseQueries cases, CancellationToken ct) =>
            await cases.GetAsync(id, ct) is { } detail ? Results.Ok(detail) : Results.NotFound());
        api.MapGet("/cases", async (ICaseQueries cases, [FromQuery(Name = "q")] string? query, [FromQuery] int? period, [FromQuery] CaseType? type, CancellationToken ct, [FromQuery] bool votes = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            Results.Ok(await cases.SearchAsync(new CaseFilter(query, period, type, votes), Clamp(page), ClampSize(pageSize), ct)));
        api.MapGet("/topics", async (ITopicQueries topics, [FromQuery(Name = "q")] string? query, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            Results.Ok(await topics.SearchAsync(query, Clamp(page), ClampSize(pageSize), ct)));
        api.MapGet("/topics/{id:int}", async (int id, ITopicQueries topics, [FromQuery] int? period, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            await topics.GetAsync(id, period, Clamp(page), ClampSize(pageSize), ct) is { } topic ? Results.Ok(topic) : Results.NotFound());
        api.MapGet("/sessions", async (ISessionQueries sessions, CancellationToken ct) => Results.Ok(await sessions.ListAsync(ct)));
        api.MapGet("/sessions/{id:int}", async (int id, ISessionQueries sessions, CancellationToken ct) =>
            await sessions.GetAsync(id, ct) is { } session ? Results.Ok(session) : Results.NotFound());
        api.MapGet("/donors", async (IDonorQueries donors, [FromQuery(Name = "q")] string? query, [FromQuery] string? party, [FromQuery] int? year, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            Results.Ok(await donors.SearchAsync(new DonorFilter(query, party, year), Clamp(page), ClampSize(pageSize), ct)));
        api.MapGet("/compare", async (IComparisonQueries comparisons, [FromQuery] int a, [FromQuery] int b, [FromQuery] int? period, CancellationToken ct) =>
            await comparisons.CompareAsync(a, b, period, ct) is { } result ? Results.Ok(result) : Results.NotFound());

        api.MapGet("/questions", async (IQuestionQueries questions, [FromQuery(Name = "q")] string? query, [FromQuery] int? period, [FromQuery] int? asker, [FromQuery(Name = "answeredby")] int? answeredBy, [FromQuery] string? minister, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            Results.Ok(await questions.SearchAsync(new QuestionFilter(query, period, asker, answeredBy, minister), Clamp(page), ClampSize(pageSize), ct)));
        api.MapGet("/questions/stats", async (IQuestionQueries questions, [FromQuery] int? period, CancellationToken ct) =>
            Results.Ok(new { Stats = await questions.GetStatsAsync(new QuestionFilter(PeriodId: period), ct), ByMinister = await questions.GetByMinisterAsync(period, ct) }));
        api.MapGet("/parties/switches", async (IPartyQueries parties, CancellationToken ct) => Results.Ok(await parties.GetSwitchesAsync(ct)));
        api.MapGet("/parties/compare", async (IPartyQueries parties, [FromQuery] string a, [FromQuery] string b, [FromQuery] int? period, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            await parties.CompareAsync(a, b, period, Clamp(page), ClampSize(pageSize), ct) is { } c ? Results.Ok(c) : Results.NotFound());
        api.MapGet("/sessions/{id:int}/dissents", async (int id, ISessionQueries sessions, CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            Results.Ok(await sessions.GetDissentsAsync(id, Clamp(page), ClampSize(pageSize), ct)));
        api.MapGet("/composition", async (ICompositionQueries composition, CancellationToken ct) => Results.Ok(await composition.GetCurrentAsync(ct)));
        api.MapGet("/data-quality", async (IDataQualityQueries quality, CancellationToken ct) => Results.Ok(await quality.GetAsync(ct)));
        api.MapGet("/search", async (ISearchQueries search, [FromQuery(Name = "q")] string query, CancellationToken ct) => Results.Ok(await search.SearchAsync(query, ct)));

        app.MapGet("/feed.xml", async (IVoteQueries votes, IPoliticianQueries politicians, ITopicQueries topics, HttpContext http, [FromQuery(Name = "politiker")] int? politician, [FromQuery(Name = "emne")] int? topic, CancellationToken ct) =>
        {
            var baseUrl = BaseUrl(http);
            if (politician is { } pid)
            {
                var profile = await politicians.GetProfileAsync(pid, ct);
                if (profile is null)
                {
                    return Results.NotFound();
                }

                var ballots = await politicians.GetBallotsAsync(pid, new BallotFilter(), 1, 50, ct);
                return Results.Text(Feeds.AtomForBallots(profile, ballots.Items, baseUrl), "application/atom+xml", Encoding.UTF8);
            }

            if (topic is { } tid)
            {
                var detail = await topics.GetAsync(tid, null, 1, 50, ct);
                return detail is null
                    ? Results.NotFound()
                    : Results.Text(Feeds.Atom(detail.Votes.Items, baseUrl, $"Afstemninger om {detail.Name}", $"/feed.xml?emne={tid}"), "application/atom+xml", Encoding.UTF8);
            }

            var latest = await votes.SearchAsync(new VoteFilter(), 1, 50, ct);
            return Results.Text(Feeds.Atom(latest.Items, baseUrl), "application/atom+xml", Encoding.UTF8);
        });
        app.MapGet("/sitemap.xml", async (ISiteQueries site, HttpContext http, CancellationToken ct) =>
            Results.Text(Feeds.Sitemap(await site.GetSitemapAsync(ct), BaseUrl(http)), "application/xml", Encoding.UTF8));
        app.MapGet("/robots.txt", (HttpContext http) => Results.Text($"User-agent: *\nAllow: /\nSitemap: {BaseUrl(http)}/sitemap.xml\n", "text/plain"));

        return app;
    }

    private static string BaseUrl(HttpContext http)
    {
        var proto = http.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? http.Request.Scheme;
        var host = http.Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? http.Request.Host.Value ?? "localhost";
        return $"{proto}://{host}";
    }

    private static int Clamp(int page) => page < 1 ? 1 : page;

    private static int ClampSize(int pageSize) => pageSize is < 1 or > 200 ? 50 : pageSize;

    internal static string ToCsv(IReadOnlyList<PoliticianBallotRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("vote_id,date,case_number,title,vote_type,passed,ballot,party,party_majority,dissents_from_party");
        foreach (var r in rows)
        {
            sb.Append(r.VoteId).Append(',')
              .Append(r.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
              .Append(Quote(r.CaseNumber)).Append(',')
              .Append(Quote(r.Title)).Append(',')
              .Append(r.VoteType).Append(',')
              .Append(r.Passed ? "true" : "false").Append(',')
              .Append(r.Ballot).Append(',')
              .Append(Quote(r.PartyShortName)).Append(',')
              .Append(r.PartyMajority?.ToString() ?? string.Empty).Append(',')
              .Append(r.DissentsFromParty ? "true" : "false")
              .AppendLine();
        }

        return sb.ToString();
    }

    private static string Quote(string? value) =>
        value is null ? string.Empty : "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
