using System.Globalization;
using System.Text;
using System.Xml;
using FolketingetVotes.Core.ReadModels;

namespace FolketingetVotes.Web.Api;

/// <summary>Atom feed of the latest votes and the XML sitemap.</summary>
internal static class Feeds
{
    public static string Atom(IReadOnlyList<VoteListItem> votes, string baseUrl)
    {
        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = false, Encoding = Encoding.UTF8 }))
        {
            w.WriteStartElement("feed", "http://www.w3.org/2005/Atom");
            w.WriteElementString("title", "Folketingets afstemninger");
            w.WriteElementString("id", baseUrl + "/feed.xml");
            w.WriteStartElement("link"); w.WriteAttributeString("href", baseUrl + "/feed.xml"); w.WriteAttributeString("rel", "self"); w.WriteEndElement();
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
