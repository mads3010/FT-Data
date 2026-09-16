using System.Net;
using System.Text;
using FolketingetVotes.Data.Oda;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FolketingetVotes.Data.Tests;

public class OdaClientTests
{
    [Fact]
    public async Task Follows_next_links_and_deserialises_danish_property_names()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["/api/Stemme?%24orderby=id&%24top=100&%24format=json"] =
                """{"odata.metadata":"x","value":[{"id":1,"typeid":1,"afstemningid":10,"aktørid":5,"opdateringsdato":"2014-09-09T09:05:59.653"}],"odata.nextLink":"https://oda.ft.dk/api/Stemme?$top=100&$format=json&$skip=100"}""",
            ["/api/Stemme?$top=100&$format=json&$skip=100"] =
                """{"odata.metadata":"x","value":[{"id":2,"typeid":3,"afstemningid":10,"aktørid":6,"opdateringsdato":"2014-09-09T09:05:59.653"}]}""",
        });
        using var client = CreateClient(handler);

        var rows = new List<OdaStemme>();
        await foreach (var row in client.EnumerateAsync<OdaStemme>("Stemme", null, "id"))
        {
            rows.Add(row);
        }

        Assert.Equal([1, 2], rows.Select(r => r.Id));
        Assert.Equal(5, rows[0].ActorId);
        Assert.Equal(3, rows[1].TypeId);
        Assert.Equal(new DateTime(2014, 9, 9, 9, 5, 59, 653), rows[0].UpdatedAt);
    }

    [Fact]
    public async Task Keyset_paging_reissues_filter_from_last_id_and_respects_upper_bound()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["/api/Afstemning?%24filter=id%20gt%200%20and%20id%20le%20500&%24orderby=id&%24top=100&%24format=json"] =
                """{"value":[{"id":100,"nummer":1,"konklusion":null,"vedtaget":true,"kommentar":null,"mødeid":1,"typeid":1,"sagstrinid":null,"opdateringsdato":"2020-01-01T00:00:00"}]}""",
            ["/api/Afstemning?%24filter=id%20gt%20100%20and%20id%20le%20500&%24orderby=id&%24top=100&%24format=json"] =
                """{"value":[]}""",
        });
        using var client = CreateClient(handler);

        var rows = new List<OdaAfstemning>();
        await foreach (var row in client.EnumerateByIdAsync<OdaAfstemning>("Afstemning", 0, 500))
        {
            rows.Add(row);
        }

        Assert.Single(rows);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Count_reads_inline_count()
    {
        var handler = new StubHandler(new Dictionary<string, string>
        {
            ["/api/Stemme?%24top=1&%24inlinecount=allpages&%24format=json"] = """{"odata.count":"1839500","value":[{"id":1,"typeid":1,"afstemningid":1,"aktørid":1,"opdateringsdato":"2014-09-09T09:05:59.653"}]}""",
        });
        using var client = CreateClient(handler);
        Assert.Equal(1_839_500, await client.CountAsync("Stemme"));
    }

    [Fact]
    public void Formats_datetime_filter_literal()
        => Assert.Equal("datetime'2026-09-01T12:30:00.000'", OdaClient.FormatDateTime(new DateTime(2026, 9, 1, 12, 30, 0)));

    private static OdaClient CreateClient(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://oda.ft.dk/api/") };
        return new OdaClient(http, Options.Create(new OdaOptions()), NullLogger<OdaClient>.Instance);
    }

    private sealed class StubHandler(Dictionary<string, string> responses) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.RequestUri!.PathAndQuery;
            Requests.Add(key);
            if (!responses.TryGetValue(key, out var body))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("no stub for " + key) });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
