using System.Net;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Core.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FolketingetVotes.Web.Tests;

/// <summary>Renders the pages against in-memory fakes of the query services (no database needed).</summary>
public class PageSmokeTests : IClassFixture<PageSmokeTests.Factory>
{
    private readonly HttpClient _client;

    public PageSmokeTests(Factory factory) => _client = factory.CreateClient();

    [Theory]
    [InlineData("/", "Hvem stemte for")]
    [InlineData("/afstemninger", "Prøvesag")]
    [InlineData("/afstemninger/1000", "Fordeling pr. parti")]
    [InlineData("/politikere", "Anna Rødsen")]
    [InlineData("/politikere/1", "Stemmehistorik")]
    [InlineData("/partier", "Røde Parti")]
    [InlineData("/partier/RØD", "Fremmøde og sammenhold")]
    [InlineData("/sager/500", "Hvem står bag")]
    [InlineData("/om", "metoden")]
    public async Task Pages_render(string path, string expectedText)
    {
        var response = await _client.GetAsync(new Uri(path, UriKind.Relative));
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(html.Contains(expectedText, StringComparison.Ordinal), $"'{expectedText}' not found in:\n{html}");
    }

    [Theory]
    [InlineData("/afstemninger/999999")]
    [InlineData("/politikere/999999")]
    [InlineData("/partier/NOPE")]
    [InlineData("/sager/999999")]
    public async Task Missing_entities_return_404(string path)
    {
        var response = await _client.GetAsync(new Uri(path, UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Api_returns_json_and_csv()
    {
        var json = await _client.GetAsync(new Uri("/api/v1/votes/1000", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, json.StatusCode);
        Assert.Contains("\"parties\"", await json.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var csv = await _client.GetAsync(new Uri("/api/v1/politicians/1/ballots?format=csv", UriKind.Relative));
        Assert.Equal("text/csv", csv.Content.Headers.ContentType?.MediaType);
        var body = await csv.Content.ReadAsStringAsync();
        Assert.StartsWith("vote_id,date,case_number", body, StringComparison.Ordinal);
        Assert.Contains("\"L 1\"", body, StringComparison.Ordinal);

        var missing = await _client.GetAsync(new Uri("/api/v1/votes/1", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IVoteQueries>();
                services.RemoveAll<IPoliticianQueries>();
                services.RemoveAll<IPartyQueries>();
                services.RemoveAll<ICaseQueries>();
                services.RemoveAll<ISiteQueries>();
                services.AddSingleton<IVoteQueries, FakeData>();
                services.AddSingleton<IPoliticianQueries, FakeData>();
                services.AddSingleton<IPartyQueries, FakeData>();
                services.AddSingleton<ICaseQueries, FakeData>();
                services.AddSingleton<ISiteQueries, FakeData>();
            });
        }
    }

    private sealed class FakeData : IVoteQueries, IPoliticianQueries, IPartyQueries, ICaseQueries, ISiteQueries
    {
        private static readonly DateTime Date = new(2024, 3, 5, 10, 0, 0);
        private static readonly VoteListItem Vote = new(1000, Date, VoteType.FinalPassage, true, 500, "L 1", "Prøvesag", "3. behandling", 2, 1, 0, 1);
        private static readonly CaseSummary Case = new(500, CaseType.Bill, "Forslag til lov om prøvesager", "Prøvesag", "L 1", 10, "Vedtaget", 1, "20231", "2023-24");

        public Task<PagedResult<VoteListItem>> SearchAsync(VoteFilter filter, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<VoteListItem>([Vote], 1, pageSize, 1));

        public Task<VoteDetail?> GetAsync(int voteId, CancellationToken ct = default)
            => Task.FromResult(voteId == 1000
                ? new VoteDetail(Vote, "Forslaget er vedtaget.", null, 100, "Møde i Salen", 1, "2023-24", Case,
                    [new PartyVoteBreakdown("RØD", "Røde Parti", 2, 1, 0, 0), new PartyVoteBreakdown("BLÅ", "Blå Parti", 0, 0, 0, 1)],
                    [new BallotRow(1, "Anna Rødsen", "RØD", BallotType.For, false), new BallotRow(3, "Rebel Rødsen", "RØD", BallotType.Against, true), new BallotRow(4, "Dorte Blåsen", "BLÅ", BallotType.Absent, false)])
                : null);

        public Task<PagedResult<PoliticianListItem>> SearchAsync(PoliticianFilter filter, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<PoliticianListItem>([new PoliticianListItem(1, "Anna Rødsen", "RØD", null, true, 1, 1.0)], 1, pageSize, 1));

        public Task<PoliticianProfile?> GetProfileAsync(int actorId, CancellationToken ct = default)
            => Task.FromResult(actorId == 1
                ? new PoliticianProfile(1, "Anna Rødsen", null, "RØD", true,
                    [new PartyMembershipRow("RØD", "Røde Parti", new DateOnly(2023, 10, 3), null)],
                    new PoliticianStats(1, 1, 0, 0, 0, 1, 0),
                    [new PoliticianPeriodStatsRow(1, "2023-24", new DateTime(2023, 10, 3), new PoliticianStats(1, 1, 0, 0, 0, 1, 0))])
                : null);

        public Task<PagedResult<PoliticianBallotRow>> GetBallotsAsync(int actorId, BallotFilter filter, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<PoliticianBallotRow>(
                [new PoliticianBallotRow(1000, Date, "Prøvesag", "L 1", 500, VoteType.FinalPassage, true, BallotType.For, "RØD", BallotType.For)], 1, pageSize, 1));

        public Task<IReadOnlyList<PartyListItem>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PartyListItem>>([new PartyListItem("RØD", "Røde Parti", 3, new DateOnly(2023, 10, 3), null)]);

        public Task<PartyDetail?> GetAsync(string shortName, CancellationToken ct = default)
            => Task.FromResult(shortName == "RØD"
                ? new PartyDetail("RØD", "Røde Parti", [new PartyMemberRow(1, "Anna Rødsen", null, new DateOnly(2023, 10, 3))],
                    [new PartyPeriodStatsRow(1, "2023-24", new DateTime(2023, 10, 3), 3, 3, 3, 2, 1)], [])
                : null);

        Task<CaseDetail?> ICaseQueries.GetAsync(int caseId, CancellationToken ct)
            => Task.FromResult(caseId == 500
                ? new CaseDetail(Case, "Et resumé.", "Forslaget blev vedtaget.", 123, new DateTime(2024, 3, 20), null,
                    [new CaseActorRow(1, "Anna Rødsen", 19, "Forslagsstiller (reg.)", ActorType.Person)],
                    [new CaseStepRow(700, "3. behandling", new DateTime(2024, 3, 5), "3. behandling", "Vedtaget")],
                    [Vote])
                : null);

        public Task<SiteOverview> GetOverviewAsync(CancellationToken ct = default)
            => Task.FromResult(new SiteOverview(DateTime.UtcNow, 1, 4, 4, Date, Date, [Vote]));

        public Task<IReadOnlyList<PeriodOption>> GetPeriodsWithVotesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PeriodOption>>([new PeriodOption(1, "20231", "2023-24", new DateTime(2023, 10, 3))]);
    }
}
