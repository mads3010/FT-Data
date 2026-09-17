using System.Net;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Core.Entities;
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
    [InlineData("/sager?q=pr%C3%B8ve", "Prøvesag")]
    [InlineData("/emner", "prøveemne")]
    [InlineData("/emner/9", "Partiernes flertal")]
    [InlineData("/folketingsaar", "2023-24")]
    [InlineData("/folketingsaar/1", "stemte grupperne ens")]
    [InlineData("/bidrag", "Prøvegiver")]
    [InlineData("/bidrag/pr%C3%B8vegiver-a-s", "Prøvegiver A/S")]
    [InlineData("/sammenlign?a=1&b=3", "Stemte ens")]
    [InlineData("/spoergsmaal", "Prøvespørgsmål")]
    [InlineData("/spoergsmaal?period=1", "Pr. minister")]
    [InlineData("/partiskift", "Rebel Rødsen")]
    [InlineData("/partier/forskel?a=RØD&b=BLÅ", "Stemte forskelligt")]
    [InlineData("/folketingsaar/1/afvigelser", "Afvigelser fra gruppen")]
    [InlineData("/sammensaetning", "Medianalder")]
    [InlineData("/status", "Datakvalitet")]
    [InlineData("/soeg?q=anna", "Anna Rødsen")]
    [InlineData("/api-docs", "openapi")]
    [InlineData("/feed.xml?politiker=1", "stemmer i Folketinget")]
    [InlineData("/feed.xml?emne=9", "prøveemne")]
    [InlineData("/openapi/v1.json", "\"openapi\"")]
    [InlineData("/udforsk", "Udforsk data")]
    [InlineData("/udforsk?metric=Absence&parties=RØD,BLÅ&chart=Pie", "Lagkagen viser")]
    [InlineData("/udforsk?metric=Attendance&grouping=Month&chart=Line", "<svg")]
    [InlineData("/api/v1/explore?metric=Attendance&format=csv", "series,label,value,basis")]
    [InlineData("/feed.xml", "<feed")]
    [InlineData("/sitemap.xml", "/afstemninger/1000")]
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
    [InlineData("/emner/999999")]
    [InlineData("/folketingsaar/999999")]
    [InlineData("/bidrag/ukendt")]
    [InlineData("/folketingsaar/999999/afvigelser")]
    [InlineData("/feed.xml?politiker=999999")]
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
                services.RemoveAll<ITopicQueries>();
                services.RemoveAll<ISessionQueries>();
                services.RemoveAll<IDonorQueries>();
                services.RemoveAll<IComparisonQueries>();
                services.RemoveAll<IQuestionQueries>();
                services.RemoveAll<ICompositionQueries>();
                services.RemoveAll<IDataQualityQueries>();
                services.RemoveAll<ISearchQueries>();
                services.RemoveAll<IExplorerQueries>();
                services.AddSingleton<IExplorerQueries, FakeData>();
                services.AddSingleton<IQuestionQueries, FakeData>();
                services.AddSingleton<ICompositionQueries, FakeData>();
                services.AddSingleton<IDataQualityQueries, FakeData>();
                services.AddSingleton<ISearchQueries, FakeData>();
                services.AddSingleton<ITopicQueries, FakeData>();
                services.AddSingleton<ISessionQueries, FakeData>();
                services.AddSingleton<IDonorQueries, FakeData>();
                services.AddSingleton<IComparisonQueries, FakeData>();
                services.AddSingleton<IVoteQueries, FakeData>();
                services.AddSingleton<IPoliticianQueries, FakeData>();
                services.AddSingleton<IPartyQueries, FakeData>();
                services.AddSingleton<ICaseQueries, FakeData>();
                services.AddSingleton<ISiteQueries, FakeData>();
            });
        }
    }

    private sealed class FakeData : IVoteQueries, IPoliticianQueries, IPartyQueries, ICaseQueries, ISiteQueries, ITopicQueries, ISessionQueries, IDonorQueries, IComparisonQueries,
        IQuestionQueries, ICompositionQueries, IDataQualityQueries, ISearchQueries, IExplorerQueries
    {
        public Task<ExplorerResult> RunAsync(ExplorerQuery query, CancellationToken ct = default)
            => Task.FromResult(new ExplorerResult(query with { From = new DateOnly(2026, 3, 1), To = new DateOnly(2026, 9, 1) }, ExplorerMath.Label(query.Metric),
                [new ExplorerSeries("RØD", "Røde Parti", [new ExplorerPoint("2026-03", "2026-03", 0.6, 100), new ExplorerPoint("2026-04", "2026-04", 0.7, 80)]),
                 new ExplorerSeries("BLÅ", "Blå Parti", [new ExplorerPoint("2026-03", "2026-03", 0.4, 50), new ExplorerPoint("2026-04", "2026-04", 0.5, 40)])],
                [], null));

        public Task<(int Id, string Name)?> ResolveKeywordAsync(string name, CancellationToken ct = default) => Task.FromResult<(int, string)?>(null);

        private static readonly QuestionListItem Question = new(600, "S 1", "Prøvespørgsmål til ministeren?", 3, "Rebel Rødsen", "RØD", "prøveministeren", 1, "Anna Rødsen",
            new DateTime(2024, 2, 1), new DateTime(2024, 2, 8), false, false, "2023-24", "20231");

        public Task<PagedResult<QuestionListItem>> SearchAsync(QuestionFilter filter, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<QuestionListItem>([Question], 1, pageSize, 1));

        public Task<QuestionStats> GetStatsAsync(QuestionFilter filter, CancellationToken ct = default)
            => Task.FromResult(new QuestionStats(1, 1, 0, 0, 7));

        public Task<IReadOnlyList<MinisterQuestionRow>> GetByMinisterAsync(int? periodId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<MinisterQuestionRow>>([new MinisterQuestionRow("prøveministeren", 1, "Anna Rødsen", 1, 7)]);

        public Task<CompositionReport> GetCurrentAsync(CancellationToken ct = default)
            => Task.FromResult(new CompositionReport(new DateOnly(2026, 9, 16),
                [new CompositionRow("RØD", "Røde Parti", 3, 2, 1, 0, 45, 6.5)], new CompositionRow("Alle", "Folketinget", 3, 2, 1, 0, 45, 6.5)));

        Task<DataQualityReport> IDataQualityQueries.GetAsync(CancellationToken ct)
            => Task.FromResult(new DataQualityReport([new SyncStateRow("Afstemning", true, DateTime.UtcNow, DateTime.Now, 1)], 1, 4, 1, 1, 0, [new BallotsPerVoteRow(4, 1)], 1, 0, 3, Date));

        Task<SearchResults> ISearchQueries.SearchAsync(string query, CancellationToken ct)
            => Task.FromResult(new SearchResults(query, [Anna], [new CaseListItem(500, CaseType.Bill, "L 1", "Prøvesag", "Vedtaget", 1, "2023-24", 1, null)], [new TopicListItem(9, "prøveemne", 3, 1, 1)], [], [Question]));

        public Task<IReadOnlyList<PartySwitchRow>> GetSwitchesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PartySwitchRow>>([new PartySwitchRow(3, "Rebel Rødsen", "BLÅ", "RØD", new DateOnly(2023, 11, 1))]);

        public Task<PartyComparison?> CompareAsync(string partyA, string partyB, int? periodId, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(partyA == "RØD" && partyB == "BLÅ"
                ? new PartyComparison("RØD", "BLÅ", "Røde Parti", "Blå Parti", 1, 0, new PagedResult<PartyDifferenceRow>([new PartyDifferenceRow(Vote, BallotType.For, BallotType.Against)], 1, pageSize, 1))
                : null);

        Task<PagedResult<DissentRow>> ISessionQueries.GetDissentsAsync(int periodId, int page, int pageSize, CancellationToken ct)
            => Task.FromResult(new PagedResult<DissentRow>([new DissentRow(3, "Rebel Rødsen", "RØD", Vote, BallotType.Against, BallotType.For)], 1, pageSize, 1));

        private static readonly DateTime Date = new(2024, 3, 5, 10, 0, 0);
        private static readonly VoteListItem Vote = new(1000, Date, VoteType.FinalPassage, true, 500, "L 1", "Prøvesag", "3. behandling", 2, 1, 0, 1);
        private static readonly CaseSummary Case = new(500, CaseType.Bill, "Forslag til lov om prøvesager", "Prøvesag", "L 1", 10, "Vedtaget", 1, "20231", "2023-24");

        public Task<PagedResult<VoteListItem>> SearchAsync(VoteFilter filter, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<VoteListItem>([Vote], 1, pageSize, 1));

        public Task<VoteDetail?> GetAsync(int voteId, CancellationToken ct = default)
            => Task.FromResult(voteId == 1000
                ? new VoteDetail(Vote, "Forslaget er vedtaget.", null, 100, "Møde i Salen", "1", 1, "2023-24", "20231", Case,
                    [new PartyVoteBreakdown("RØD", "Røde Parti", 2, 1, 0, 0), new PartyVoteBreakdown("BLÅ", "Blå Parti", 0, 0, 0, 1)],
                    [new BallotRow(1, "Anna Rødsen", "RØD", BallotType.For, false), new BallotRow(3, "Rebel Rødsen", "RØD", BallotType.Against, true), new BallotRow(4, "Dorte Blåsen", "BLÅ", BallotType.Absent, false)])
                : null);

        public Task<PagedResult<PoliticianListItem>> SearchAsync(PoliticianFilter filter, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<PoliticianListItem>([Anna, Rebel], 1, pageSize, 2));

        private static readonly PoliticianListItem Anna = new(1, "Anna Rødsen", "RØD", null, true, 1, 1.0, 1970);
        private static readonly PoliticianListItem Rebel = new(3, "Rebel Rødsen", "RØD", null, true, 1, 1.0, 1980);

        public Task<PoliticianProfile?> GetProfileAsync(int actorId, CancellationToken ct = default)
            => Task.FromResult(actorId == 1
                ? new PoliticianProfile(1, "Anna Rødsen", null, "RØD", true,
                    [new PartyMembershipRow("RØD", "Røde Parti", new DateOnly(2023, 10, 3), null)],
                    new PoliticianStats(1, 1, 0, 0, 0, 1, 0),
                    [new PoliticianPeriodStatsRow(1, "2023-24", new DateTime(2023, 10, 3), new PoliticianStats(1, 1, 0, 0, 0, 1, 0))],
                    1970,
                    [new RolePeriodRow(RolePeriodKind.Minister, "prøveministeren", new DateOnly(2022, 12, 15), null)],
                    [],
                    ["Prøveudvalget"],
                    [new CaseListItem(500, CaseType.Bill, "L 1", "Prøvesag", "Vedtaget", 1, "2023-24", 1, "Forslagsstiller (reg.)")],
                    [new RolePeriodRow(RolePeriodKind.Leave, "orlov med vederlag", new DateOnly(2021, 1, 1), new DateOnly(2021, 2, 1))],
                    2,
                    1)
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
                    [Vote],
                    [new KeywordRow(9, "prøveemne", 3)])
                : null);

        public Task<PagedResult<CaseListItem>> SearchAsync(CaseFilter filter, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<CaseListItem>([new CaseListItem(500, CaseType.Bill, "L 1", "Prøvesag", "Vedtaget", 1, "2023-24", 1, null)], 1, pageSize, 1));

        public Task<PagedResult<TopicListItem>> SearchAsync(string? query, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<TopicListItem>([new TopicListItem(9, "prøveemne", 3, 1, 1)], 1, pageSize, 1));

        public Task<TopicDetail?> GetAsync(int keywordId, int? periodId, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(keywordId == 9
                ? new TopicDetail(9, "prøveemne", 3, 1, [new PartyTopicPosition("RØD", "Røde Parti", 1, 0, 0)], new PagedResult<VoteListItem>([Vote], 1, pageSize, 1),
                    [new TopicSessionRow(1, "2023-24", 1, 1, 1), new TopicSessionRow(0, "2022-23", 2, 1, 0)], periodId)
                : null);

        private static readonly SessionListItem Session = new(1, "20231", "2023-24", new DateTime(2023, 10, 3), new DateTime(2024, 10, 1), 1, 1, 0);

        Task<IReadOnlyList<SessionListItem>> ISessionQueries.ListAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<SessionListItem>>([Session]);

        Task<SessionDetail?> ISessionQueries.GetAsync(int periodId, CancellationToken ct)
            => Task.FromResult(periodId == 1
                ? new SessionDetail(Session, [new SessionPartyRow("RØD", "Røde Parti", 3, 3, 3, 2, 1), new SessionPartyRow("BLÅ", "Blå Parti", 1, 1, 0, 0, 0)], [Vote], ["RØD", "BLÅ"], [new PartyAgreement("RØD", "BLÅ", 1, 0)],
                    [new LegislationRow(CaseType.Bill, "Regeringsforslag", 1, 1)], 30, 1)
                : null);

        public Task<PagedResult<DonorListItem>> SearchAsync(DonorFilter filter, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new PagedResult<DonorListItem>([new DonorListItem("prøvegiver a s", "Prøvegiver A/S", ["RØD"], 2023, 2023, 1, 10_000m, null)], 1, pageSize, 1));

        Task<DonorDetail?> IDonorQueries.GetAsync(string key, CancellationToken ct)
            => Task.FromResult(key == "prøvegiver a s"
                ? new DonorDetail(key, "Prøvegiver A/S", null, [new DonorContributionRow(2023, "Røde Parti", "RØD", "Vej 1, 1000 By", 10_000m, "Bidrag over grænsen", 4, "test.pdf", "Prøvegiver A/S, Vej 1, 1000 By 10.000 kr.")])
                : null);

        public Task<IReadOnlyList<int>> GetYearsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<int>>([2023]);

        public Task<ComparisonResult?> CompareAsync(int actorA, int actorB, int? periodId, CancellationToken ct = default)
            => Task.FromResult(actorA == 1 && actorB == 3
                ? new ComparisonResult(Anna, Rebel, 1, 1, 0, [new ComparisonDifference(Vote, BallotType.For, BallotType.Against)])
                : null);

        public Task<IReadOnlyList<SitemapEntry>> GetSitemapAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SitemapEntry>>([new SitemapEntry("/", null), new SitemapEntry("/afstemninger/1000", Date)]);

        public Task<SiteOverview> GetOverviewAsync(CancellationToken ct = default)
            => Task.FromResult(new SiteOverview(DateTime.UtcNow, 1, 4, 4, Date, Date, [Vote]));

        public Task<IReadOnlyList<PeriodOption>> GetPeriodsWithVotesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PeriodOption>>([new PeriodOption(1, "20231", "2023-24", new DateTime(2023, 10, 3))]);
    }
}
