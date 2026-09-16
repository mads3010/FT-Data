using System.Globalization;
using System.Text.RegularExpressions;
using FolketingetVotes.Core;

namespace FolketingetVotes.Data.PartyAccounts;

/// <summary>One page of extracted text.</summary>
public sealed record PdfPageText(int PageNumber, string Text);

public sealed record ParsedDonation(string DonorName, string? DonorAddress, decimal? Amount, string? Note, int PageNumber, string RawText);

public sealed record ParsedPartyAccount(string PartyName, int FirstPage, IReadOnlyList<ParsedDonation> Donations);

/// <summary>
/// Extracts disclosed private contributions from the text of Folketinget's combined annual party-accounts PDF.
/// The PDF is a scan of each party's own statement, so layouts differ; this parser is heuristic and keeps the
/// raw line for every row so results can be audited against the source page. Pure function: easy to test.
/// </summary>
public static partial class PartyAccountParser
{
    /// <summary>Party names as they appear as section headings in the accounts, mapped to group short names.</summary>
    public static readonly IReadOnlyDictionary<string, string> KnownParties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Socialdemokratiet"] = "S",
        ["Venstre"] = "V",
        ["Venstre, Danmarks Liberale Parti"] = "V",
        ["Moderaterne"] = "M",
        ["Socialistisk Folkeparti"] = "SF",
        ["SF - Socialistisk Folkeparti"] = "SF",
        ["Danmarksdemokraterne"] = "DD",
        ["Liberal Alliance"] = "LA",
        ["Det Konservative Folkeparti"] = "KF",
        ["Konservative"] = "KF",
        ["Enhedslisten"] = "EL",
        ["Radikale Venstre"] = "RV",
        ["Dansk Folkeparti"] = "DF",
        ["Alternativet"] = "ALT",
        ["Nye Borgerlige"] = "NB",
        ["Kristendemokraterne"] = "KD",
        ["Frie Grønne"] = "FG",
        ["Borgernes Parti"] = "BP",
    };

    public static IReadOnlyList<ParsedPartyAccount> Parse(IReadOnlyList<PdfPageText> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        var sections = SplitIntoPartySections(pages);
        return sections.Select(s => new ParsedPartyAccount(s.PartyName, s.FirstPage, ParseDonations(s.Lines))).ToList();
    }

    public static string? ShortNameFor(string partyName) => PartyNames.ShortNameFor(partyName);

    private static List<(string PartyName, int FirstPage, List<(int Page, string Line)> Lines)> SplitIntoPartySections(IReadOnlyList<PdfPageText> pages)
    {
        var sections = new List<(string PartyName, int FirstPage, List<(int Page, string Line)> Lines)>();
        foreach (var page in pages)
        {
            foreach (var rawLine in page.Text.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                var heading = MatchPartyHeading(line);
                if (heading is not null && (sections.Count == 0 || sections[^1].PartyName != heading))
                {
                    sections.Add((heading, page.PageNumber, []));
                }

                if (sections.Count > 0)
                {
                    sections[^1].Lines.Add((page.PageNumber, line));
                }
            }
        }

        return sections;
    }

    /// <summary>A heading is a short line that is (or starts with) a known party name, e.g. "Socialdemokratiet – Årsregnskab 2023".</summary>
    private static string? MatchPartyHeading(string line)
    {
        if (line.Length > 80)
        {
            return null;
        }

        foreach (var name in KnownParties.Keys.OrderByDescending(k => k.Length))
        {
            if (line.StartsWith(name, StringComparison.OrdinalIgnoreCase)
                && (line.Length == name.Length || !char.IsLetter(line[name.Length])))
            {
                return name;
            }
        }

        return null;
    }

    private static List<ParsedDonation> ParseDonations(List<(int Page, string Line)> lines)
    {
        var donations = new List<ParsedDonation>();
        var inDonorBlock = false;
        foreach (var (page, line) in lines)
        {
            if (DonorHeaderRegex().IsMatch(line))
            {
                inDonorBlock = true;
                continue;
            }

            if (!inDonorBlock)
            {
                continue;
            }

            if (BlockEndRegex().IsMatch(line))
            {
                inDonorBlock = false;
                continue;
            }

            var donation = ParseDonationLine(line, page);
            if (donation is not null)
            {
                donations.Add(donation);
            }
        }

        return donations;
    }

    internal static ParsedDonation? ParseDonationLine(string line, int page)
    {
        if (NoneRegex().IsMatch(line) || line.Length < 4)
        {
            return null;
        }

        var amountMatch = AmountRegex().Match(line);
        decimal? amount = null;
        var text = line;
        if (amountMatch.Success)
        {
            var digits = amountMatch.Groups["amount"].Value.Replace(".", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).Replace(',', '.');
            if (decimal.TryParse(digits, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                amount = parsed;
            }

            text = line.Remove(amountMatch.Index, amountMatch.Length).Trim().TrimEnd(',', ';', ':', '-').Trim();
        }

        if (text.Length < 3 || !text.Any(char.IsLetter))
        {
            return null;
        }

        // "Name, Street 1, 1234 City" → split on the first comma when a postal code follows.
        string? address = null;
        var name = text;
        var comma = text.IndexOf(',', StringComparison.Ordinal);
        if (comma > 0 && PostalCodeRegex().IsMatch(text[(comma + 1)..]))
        {
            name = text[..comma].Trim();
            address = text[(comma + 1)..].Trim();
        }

        return new ParsedDonation(name, address, amount, null, page, line);
    }

    [GeneratedRegex(@"(bidrag|tilskud|gave).{0,80}?\b(over|overstig\w*|mere end|større end)\b.{0,40}?\bkr", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DonorHeaderRegex();

    [GeneratedRegex(@"^(anonyme|note|noter|balance|resultatopgørelse|aktiver|passiver|egenkapital|ledelsespåtegning|revisionspåtegning|den uafhængige revisors)", RegexOptions.IgnoreCase)]
    private static partial Regex BlockEndRegex();

    [GeneratedRegex(@"^(ingen|der er ikke modtaget|partiet har ikke modtaget|-|–)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NoneRegex();

    [GeneratedRegex(@"(?<![\w,.])(?<amount>\d{1,3}(?:[. ]\d{3})+(?:,\d{1,2})?|\d{4,}(?:,\d{1,2})?)\s*(?:kr\.?|DKK)?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex AmountRegex();

    [GeneratedRegex(@"\b\d{4}\s+\p{L}")]
    private static partial Regex PostalCodeRegex();
}
