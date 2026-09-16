using System.Globalization;
using System.Text;
using System.Xml;
using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Web.Api;

/// <summary>Atom feed of the latest votes and the XML sitemap.</summary>
internal static class Feeds
{
    public static string Atom(IReadOnlyList<VoteListItem> votes, string baseUrl, string title = "Folketingets afstemninger", string selfPath = "/feed.xml")
    {
        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = false, Encoding = Encoding.UTF8 }))
        {
            w.WriteStartElement("feed", "http://www.w3.org/2005/Atom");
            w.WriteElementString("title", title);
            w.WriteElementString("id", baseUrl + selfPath);
            w.WriteStartElement("link"); w.WriteAttributeString("href", baseUrl + selfPath); w.WriteAttributeString("rel", "self"); w.WriteEndElement();
            w.WriteStartElement("link"); w.WriteAttributeString("href", baseUrl + "/"); w.WriteEndElement();
            var updated = votes.Count > 0 ? votes[0].Date : DateTime.UtcNow;
            w.WriteElementString("updated", updated.ToString("yyyy-MM-dd'T'HH:mm:ss+01:00", CultureInfo.InvariantCulture));
            foreach (var v in votes)
            {
                var url = $"{baseUrl}/afstemninger/{v.VoteId}";
                w.WriteStartElement("entry");
                w.WriteElementString("id", url);
                w.WriteElementString("title", $"{v.Title} ({(v.Passed ? "vedtaget" : "forkastet")} {v.ForCount}–{v.AgainstCount})");
                w.WriteStartElement("link"); w.WriteAttributeString("href", url); w.WriteEndElement();
                w.WriteElementString("updated", v.Date.ToString("yyyy-MM-dd'T'HH:mm:ss+01:00", CultureInfo.InvariantCulture));
                w.WriteElementString("summary", $"{v.CaseNumber} · {v.StepTitle}: for {v.ForCount}, imod {v.AgainstCount}, hverken {v.AbstainCount}, fraværende {v.AbsentCount}.");
                w.WriteEndElement();
            }

            w.WriteEndElement();
        }

        return sb.ToString();
    }

    /// <summary>A member's latest ballots as a feed.</summary>
    public static string AtomForBallots(PoliticianProfile profile, IReadOnlyList<PoliticianBallotRow> ballots, string baseUrl)
    {
        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 }))
        {
            var self = $"/feed.xml?politiker={profile.ActorId}";
            w.WriteStartElement("feed", "http://www.w3.org/2005/Atom");
            w.WriteElementString("title", $"{profile.Name}: stemmer i Folketinget");
            w.WriteElementString("id", baseUrl + self);
            w.WriteStartElement("link"); w.WriteAttributeString("href", baseUrl + self); w.WriteAttributeString("rel", "self"); w.WriteEndElement();
            w.WriteStartElement("link"); w.WriteAttributeString("href", $"{baseUrl}/politikere/{profile.ActorId}"); w.WriteEndElement();
            var updated = ballots.Count > 0 ? ballots[0].Date : DateTime.UtcNow;
            w.WriteElementString("updated", updated.ToString("yyyy-MM-dd'T'HH:mm:ss+01:00", CultureInfo.InvariantCulture));
            foreach (var b in ballots)
            {
                var url = $"{baseUrl}/afstemninger/{b.VoteId}";
                w.WriteStartElement("entry");
                w.WriteElementString("id", $"{url}#{profile.ActorId}");
                w.WriteElementString("title", $"{BallotText(b.Ballot)}: {b.Title}");
                w.WriteStartElement("link"); w.WriteAttributeString("href", url); w.WriteEndElement();
                w.WriteElementString("updated", b.Date.ToString("yyyy-MM-dd'T'HH:mm:ss+01:00", CultureInfo.InvariantCulture));
                w.WriteElementString("summary", $"{b.CaseNumber} · {(b.Passed ? "vedtaget" : "forkastet")}{(b.DissentsFromParty ? " · afveg fra gruppens flertal" : string.Empty)}");
                w.WriteEndElement();
            }

            w.WriteEndElement();
        }

        return sb.ToString();
    }

    private static string BallotText(Core.Enums.BallotType type) => type switch
    {
        Core.Enums.BallotType.For => "For",
        Core.Enums.BallotType.Against => "Imod",
        Core.Enums.BallotType.Abstain => "Hverken for eller imod",
        _ => "Fraværende",
    };

    public static string Sitemap(IReadOnlyList<SitemapEntry> entries, string baseUrl)
    {
        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, new XmlWriterSettings { Indent = false, Encoding = Encoding.UTF8 }))
        {
            w.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var e in entries)
            {
                w.WriteStartElement("url");
                w.WriteElementString("loc", baseUrl + e.Path);
                if (e.LastModified is { } m)
                {
                    w.WriteElementString("lastmod", m.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                w.WriteEndElement();
            }

            w.WriteEndElement();
        }

        return sb.ToString();
    }
}
