using System.Globalization;
using System.Text.RegularExpressions;
using FolketingetVotes.Core;
using FolketingetVotes.Core.Entities;

namespace FolketingetVotes.Data.Oda;

/// <summary>
/// Extracts party terms from the <c>&lt;constituencies&gt;</c> element of a member's biography XML.
/// Entries look like "Folketingsmedlem for Venstre i Nordsjællands Storkreds, 13. november 2007 – 18. juni 2015."
/// or "... fra 15. september 2011." Pure function; unknown formats are skipped.
/// </summary>
public static partial class BiographyParser
{
    private static readonly string[] Months =
    [
        "januar", "februar", "marts", "april", "maj", "juni", "juli", "august", "september", "oktober", "november", "december",
    ];

    public static IReadOnlyList<BiographyMembership> ParseMemberships(int personId, string? biographyXml)
    {
        if (string.IsNullOrEmpty(biographyXml))
        {
            return [];
        }

        var result = new List<BiographyMembership>();
        foreach (Match entry in ConstituencyRegex().Matches(biographyXml))
        {
            var text = System.Net.WebUtility.HtmlDecode(entry.Groups[1].Value).Trim();
            var match = TermRegex().Match(text);
            if (!match.Success)
            {
                continue;
            }

            var start = ParseDate(match.Groups["from"].Value);
            if (start is null)
            {
                continue;
            }

            var end = match.Groups["to"].Success ? ParseDate(match.Groups["to"].Value) : null;
            var party = match.Groups["party"].Value.Trim();
            result.Add(new BiographyMembership
            {
                PersonId = personId,
                PartyName = party,
                PartyShortName = PartyNames.ShortNameFor(party),
                Constituency = match.Groups["kreds"].Value.Trim().TrimEnd(','),
                StartDate = start.Value,
                EndDate = end,
                IsTemporary = match.Groups["temp"].Success,
            });
        }

        return result;
    }

    internal static DateOnly? ParseDate(string text)
    {
        var match = DateRegex().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var month = Array.IndexOf(Months, match.Groups["month"].Value.ToLowerInvariant()) + 1;
        if (month == 0)
        {
            return null;
        }

        var day = int.Parse(match.Groups["day"].Value, CultureInfo.InvariantCulture);
        var year = int.Parse(match.Groups["year"].Value, CultureInfo.InvariantCulture);
        return day >= 1 && day <= DateTime.DaysInMonth(year, month) ? new DateOnly(year, month, day) : null;
    }

    [GeneratedRegex("<constituency>(.*?)</constituency>", RegexOptions.Singleline)]
    private static partial Regex ConstituencyRegex();

    // "Folketingsmedlem for {party} i {kreds}[,] [fra ]{date}[ – {date}]."
    [GeneratedRegex(@"^(?<temp>[Mm]idlertidigt\s+)?[Ff]olketingsmedlem\s+for\s+(?<party>.+?)\s+i\s+(?<kreds>.+?),?\s+(?:fra\s+)?(?<from>\d{1,2}\.\s*\p{L}+\s+\d{4})(?:\s*[–\-]\s*(?<to>\d{1,2}\.\s*\p{L}+\s+\d{4}))?\s*\.?\s*$", RegexOptions.Singleline)]
    private static partial Regex TermRegex();

    [GeneratedRegex(@"(?<day>\d{1,2})\.\s*(?<month>\p{L}+)\s+(?<year>\d{4})")]
    private static partial Regex DateRegex();
}
