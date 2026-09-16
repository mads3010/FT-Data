using System.Globalization;
using System.Text.RegularExpressions;
using FolketingetVotes.Core;

namespace FolketingetVotes.Data.PartyAccounts;

/// <summary>One page of extracted (or OCR'd) text.</summary>
public sealed record PdfPageText(int PageNumber, string Text);

public sealed record ParsedDonation(string DonorName, string? DonorAddress, decimal? Amount, string? Note, int PageNumber, string RawText);

public sealed record ParsedPartyAccount(string PartyName, int FirstPage, IReadOnlyList<ParsedDonation> Donations);

/// <summary>
/// Extracts the disclosed private contributions from the text of Folketinget's combined annual party-accounts
/// PDF. The PDF concatenates each party's own annual report, so layouts differ per party; the rules below were
/// derived from the 2019–2024 files (see docs/features.md § Party accounts). Every row keeps its raw line and
/// page number so it can be checked against the source. Pure function: easy to test.
/// </summary>
public static partial class PartyAccountParser
{
    /// <summary>Organisations that appear as sections in the combined PDF but are not parliamentary groups.</summary>
    private static readonly string[] ExtraSectionNames = ["Folkebevægelsen mod EU", "Venstres Landsorganisation", "Dansk Folkepartis Landsorganisation", "Dansk Folkeparti Landsorganisation", "Liberal Alliance Landsorganisation", "Danmarks Nye Venstrefløjsparti", "Radikale Venstres Landsforbund", "Det Radikale Venstres Landsforbund"];

    private static readonly string[] SectionNames = [.. PartyNames.Names.Concat(ExtraSectionNames).Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(n => n.Length)];

    /// <summary>Running headers/cover titles sit in the first lines of a page, running footers in the last; only those may switch the section.</summary>
    private const int HeadingLinesAtTop = 6;
    private const int HeadingLinesAtBottom = 3;

    public static IReadOnlyList<ParsedPartyAccount> Parse(IReadOnlyList<PdfPageText> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);

        // A party can head several runs of pages under name variants ("Socialdemokratiet", "Socialdemokratiet i Danmark",
        // running headers, auditor pages); merge by short name when known, otherwise by name.
        return SplitIntoSections(pages)
            .GroupBy(s => ShortNameFor(s.PartyName) ?? s.PartyName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ParsedPartyAccount(
                g.OrderBy(s => s.FirstPage).First().PartyName,
                g.Min(s => s.FirstPage),
                Deduplicate(g.OrderBy(s => s.FirstPage).SelectMany(s => ParseDonations(s.Lines)).ToList())))
            .OrderBy(a => a.FirstPage)
            .ToList();
    }

    public static string? ShortNameFor(string partyName) => PartyNames.ShortNameFor(partyName);

    internal const string UnreadableName = "(navn ikke læsbart i kilden)";

    /// <summary>
    /// Some reports list the same donors twice (management report and notes); keep the first occurrence of an
    /// identical (name, amount, postal code) row, and drop nameless rows that duplicate a named one.
    /// </summary>
    private static List<ParsedDonation> Deduplicate(List<ParsedDonation> rows)
    {
        var named = rows.Where(r => r.DonorName != UnreadableName).ToList();
        var seen = new HashSet<(string, decimal?, string?)>();
        var result = new List<ParsedDonation>();
        foreach (var row in rows)
        {
            var key = (row.DonorName.ToUpperInvariant(), row.Amount, PostalOf(row.DonorAddress));
            if (!seen.Add(key))
            {
                continue;
            }

            if (row.DonorName == UnreadableName && row.Amount is not null
                && named.Any(n => n.Amount == row.Amount && PostalOf(n.DonorAddress) is { } p && p == PostalOf(row.DonorAddress)))
            {
                continue;
            }

            result.Add(row);
        }

        return result;
    }

    private static string? PostalOf(string? address) => address is null ? null : PostalCodeRegex().Match(address) is { Success: true } m ? m.Value[..4] : null;

    private sealed record Section(string PartyName, int FirstPage, List<(int Page, string Line)> Lines);

    private static List<Section> SplitIntoSections(IReadOnlyList<PdfPageText> pages)
    {
        var sections = new List<Section>();
        foreach (var page in pages)
        {
            var lines = page.Text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            var candidates = lines.Take(HeadingLinesAtTop).Concat(lines.Skip(Math.Max(HeadingLinesAtTop, lines.Count - HeadingLinesAtBottom)));
            // A running header or cover title is short and carries no amounts ("Tilskud til Folkebevægelsen mod EU  1.000.000" is a ledger line).
            var heading = candidates.Where(l => l.Length <= 60 && !TrailingAmountRegex().IsMatch(l) && !MoneyColumnsRegex().IsMatch(l)).Select(MatchSectionName).FirstOrDefault(h => h is not null);
            if (heading is not null && (sections.Count == 0 || !string.Equals(sections[^1].PartyName, heading, StringComparison.OrdinalIgnoreCase)))
            {
                sections.Add(new Section(heading, page.PageNumber, []));
            }

            if (sections.Count > 0)
            {
                sections[^1].Lines.AddRange(lines.Select(l => (page.PageNumber, l)));
            }
        }

        return sections;
    }

    private static string? MatchSectionName(string line)
    {
        foreach (var name in SectionNames)
        {
            var index = line.IndexOf(name, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var startOk = index == 0 || !char.IsLetter(line[index - 1]);
            var end = index + name.Length;
            var endOk = end == line.Length || !char.IsLetter(line[end]);
            if (startOk && endOk)
            {
                return name;
            }
        }

        return null;
    }

    private static List<ParsedDonation> ParseDonations(List<(int Page, string Line)> lines)
    {
        var donations = new List<ParsedDonation>();
        var window = new Queue<string>();
        string? blockNote = null;
        var misses = 0;
        string? pendingName = null;
        var pendingPage = 0;
        decimal? pendingAmount = null;

        foreach (var (page, rawLine) in lines)
        {
            var line = CleanLine(rawLine);
            if (line.Length == 0 || IsNoise(line))
            {
                continue;
            }

            window.Enqueue(line);
            if (window.Count > 3)
            {
                window.Dequeue();
            }

            if (blockNote is null)
            {
                if (IsHeader(line, window))
                {
                    blockNote = Trim(string.Join(' ', window.Where(IsHeaderish)), 240);
                    misses = 0;
                }

                continue;
            }

            if (IsBlockEnd(line))
            {
                blockNote = null;
                pendingName = null;
                continue;
            }

            if (IsHeader(line, window) || IsSubHeader(line))
            {
                blockNote = Trim(line.TrimEnd(':'), 240);
                pendingName = null;
                continue;
            }

            if (AmountLineRegex().Match(line) is { Success: true } amountLine)
            {
                var amount = ParseAmount(amountLine.Groups["amount"].Value);
                if (donations.Count > 0 && donations[^1].Amount is null && pendingName is null)
                {
                    donations[^1] = donations[^1] with { Amount = amount, RawText = donations[^1].RawText + " | " + line };
                }
                else
                {
                    pendingAmount = amount; // "Beløb: 75.000 kr." stated before the donor ("Modtaget fra:" …)
                }

                continue;
            }

            if (PostalOnlyRegex().IsMatch(line) && donations.Count > 0 && pendingName is null && PostalOf(donations[^1].DonorAddress) is null)
            {
                var last = donations[^1];
                donations[^1] = last with { DonorAddress = last.DonorAddress is null ? line : last.DonorAddress + ", " + line, RawText = last.RawText + " | " + line };
                continue;
            }

            if (TryParseRow(line, page, blockNote, pendingName, donations.Count > 0 ? donations[^1].DonorAddress : null, out var donation))
            {
                if (pendingName is not null && donation.DonorName == pendingName)
                {
                    donation = donation with { RawText = pendingName + " | " + line, PageNumber = pendingPage };
                }

                if (donation.Amount is null && pendingAmount is not null)
                {
                    donation = donation with { Amount = pendingAmount };
                    pendingAmount = null;
                }

                donations.Add(donation);
                pendingName = null;
                misses = 0;
                continue;
            }

            if (donations.Count > 0 && IsNameContinuation(line) && donations[^1].DonorName.EndsWith('-'))
            {
                var last = donations[^1];
                donations[^1] = last with { DonorName = last.DonorName[..^1] + line, RawText = last.RawText + " | " + line };
                continue;
            }

            if (IsNameOnly(line))
            {
                pendingName = line;
                pendingPage = page;
                continue;
            }

            if (++misses >= 3)
            {
                blockNote = null;
                pendingName = null;
            }
        }

        return donations;
    }

    /// <summary>A header introduces a donor list: within the last three lines, contribution + above-threshold wording, not a financial-statement line.</summary>
    private static bool IsHeader(string line, IEnumerable<string> window)
    {
        if (TrailingAmountRegex().IsMatch(line) && !line.EndsWith(':'))
        {
            return false; // "Donationer over 22.800 kr  279.192" is an income-statement line, not a list header
        }

        var joined = string.Join(' ', window.Where(IsHeaderish));
        return ContributionRegex().IsMatch(joined) && ThresholdRegex().IsMatch(joined) && (ContributionRegex().IsMatch(line) || ThresholdRegex().IsMatch(line) || line.EndsWith(':'));
    }

    private static bool IsHeaderish(string line) => !TrailingAmountRegex().IsMatch(line) || line.EndsWith(':');

    private static bool IsSubHeader(string line) => SubHeaderRegex().IsMatch(line);

    private static bool IsBlockEnd(string line) => BlockEndRegex().IsMatch(line) || YearColumnsRegex().IsMatch(line);

    private static bool IsNoise(string line) => NoiseRegex().IsMatch(line) || line.All(c => !char.IsLetterOrDigit(c));

    private static bool IsNameContinuation(string line) => line.Length is >= 3 and <= 50 && !line.Any(char.IsDigit) && !SentenceRegex().IsMatch(line);

    private static bool IsNameOnly(string line) =>
        line.Length is >= 3 and <= 60 && !line.Any(char.IsDigit) && !line.EndsWith(':') && !SentenceRegex().IsMatch(line) && line.Count(char.IsLetter) >= 3
        && !line.Contains("  ", StringComparison.Ordinal) && !line.Contains(',', StringComparison.Ordinal);

    internal static bool TryParseRow(string line, int page, string? note, string? pendingName, string? previousAddress, out ParsedDonation donation)
    {
        donation = null!;
        if (ContributionRegex().IsMatch(line) && ThresholdRegex().IsMatch(line))
        {
            return false; // a header, even when it ends in a threshold amount
        }

        if (SentenceRegex().IsMatch(line) && !TrailingAmountRegex().IsMatch(line))
        {
            return false;
        }

        var text = BulletRegex().Replace(line, string.Empty).Trim();
        var hasPostal = PostalCodeRegex().IsMatch(text);
        var amountMatch = TrailingAmountRegex().Match(text);
        var hasStreet = StreetNumberRegex().IsMatch(text);
        if (!hasPostal && !amountMatch.Success && !hasStreet)
        {
            return false;
        }

        decimal? amount = null;
        if (amountMatch.Success)
        {
            amount = ParseAmount(amountMatch.Groups["amount"].Value);
            text = text[..amountMatch.Index].Trim().TrimEnd('*', ')', ' ');
        }

        var columns = Regex.Split(text, @"\s{2,}").Select(c => c.Trim().TrimEnd(',')).Where(c => c.Length > 0).ToList();
        string name;
        string? address;
        if (columns.Count >= 2)
        {
            name = columns[0];
            address = string.Join(", ", columns.Skip(1));
        }
        else
        {
            var comma = text.IndexOf(',', StringComparison.Ordinal);
            name = comma > 0 ? text[..comma].Trim() : text;
            address = comma > 0 ? text[(comma + 1)..].Trim() : null;
        }

        // The name column was lost (OCR) or the row is just an address for a name on the previous line.
        var nameIsAddress = PostalCodeRegex().IsMatch(name) || StreetNumberRegex().IsMatch(name) || !name.Any(char.IsLetter)
            || (previousAddress is not null && previousAddress.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        if (nameIsAddress)
        {
            address = address is null ? name : name + ", " + address;
            name = pendingName ?? UnreadableName;
        }

        if (name.Length < 2 || LabelNameRegex().IsMatch(name))
        {
            return false;
        }

        address = address?.TrimEnd('*', ' ', '.');
        donation = new ParsedDonation(name.TrimEnd('*', ' '), string.IsNullOrWhiteSpace(address) ? null : address, amount, note, page, line);
        return true;
    }

    /// <summary>Removes e-signature widget text and dangling fragments that OCR glues onto lines.</summary>
    internal static string CleanLine(string line)
    {
        var cleaned = SignatureJunkRegex().Replace(line, string.Empty);
        cleaned = DanglingParenthesisRegex().Replace(cleaned, string.Empty);
        return cleaned.Trim().TrimEnd(',', ';');
    }

    internal static decimal? ParseAmount(string digits)
    {
        var cleaned = digits.Replace(".", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max];

    // Wording that introduces the disclosure of contributions.
    [GeneratedRegex(@"\b(bidrag\w*|tilskud\w*|donation\w*|indbetalt|naturalieydelser)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ContributionRegex();

    // "over 22.800 kr.", "overstiger kr. 22.800", "mere end kr.", "større end 22,8 t.kr."
    [GeneratedRegex(@"\b(over|overstig\w*|mere end|større end|storre end)\b[^\n]{0,40}?(kr\b|kr\.|t\.kr|t\. kr|\d{1,3}[.,]\d{3}|\d{2},\d\s*t)|stille\w*\s.{0,40}?til\s+rådighed|(nedenstående|følgende)\s+(bidragyd|tilskudsyd)", RegexOptions.IgnoreCase)]
    private static partial Regex ThresholdRegex();

    // Sub-headings inside a list: "Private personer:", "Organisationer:", "Bidrag privat", "Navn  Adresse", "Tilskudsyder:  Adresse:", "Modtaget fra:"
    [GeneratedRegex(@"^(private personer|privatpersoner|organisationer|virksomheder|enkeltpersoner|bidrag privat|bidrag erhverv|navn\b.*adresse|tilskudsyder\b.*adresse|modtager\s*:|modtaget fra\s*:|\(fortsat\))", RegexOptions.IgnoreCase)]
    private static partial Regex SubHeaderRegex();

    // Lines that end a list.
    [GeneratedRegex(@"^(der er (ikke )?modtaget|der er ikke|der har ikke|anonyme (bidrag|tilskud)|ingen\b|de private og|finansielle indtægter|vedr\.|indberetningspligtig|for god ordens|\d{1,2}\.?\s+[A-ZÆØÅ][a-zæøå]+|note \d|noter\b|resultatopgørelse|balance\b|ledelsespåtegning|den uafhængige|personaleomkostninger|renteindt|partistøtte|egenkapital|\*\))", RegexOptions.IgnoreCase)]
    private static partial Regex BlockEndRegex();

    // Two or more thousand-separated figures on one line: a ledger row, never a heading.
    [GeneratedRegex(@"\d{1,3}\.\d{3}\b.*\d{1,3}\.\d{3}\b")]
    private static partial Regex MoneyColumnsRegex();

    // "2023  2022" column headers of a financial table.
    [GeneratedRegex(@"^(19|20)\d{2}\s{2,}(19|20)\d{2}\b")]
    private static partial Regex YearColumnsRegex();

    // Signature widgets, page numbers, OCR junk.
    [GeneratedRegex(@"^(penneo|this document|dette dokument|delle dokument|agreement-id|cvr[\s-]*n|side \d+|\d{1,3}$|•+$|\*+$|=== page)", RegexOptions.IgnoreCase)]
    private static partial Regex NoiseRegex();

    // Ordinary prose (statutory boilerplate) is never a donor row.
    [GeneratedRegex(@"\b(har|er|skal|jf\.?|iht\.?|henhold|modtaget|modtager|oplyse\w*|følgende|nedenstående|indberet\w*|regnskab\w*|bidragyd\w*|tilskudsyd\w*|partiregnskab\w*|offentligg\w*|anvendt|værdi|lov\b|loven|stk\.|pkt\.)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SentenceRegex();

    // Any leading bullet or dash glyph OCR produced ("-", "•", "·", "●", "▪", "*").
    [GeneratedRegex(@"^[^\p{L}\p{N}(\[]+")]
    private static partial Regex BulletRegex();

    // Column labels and totals that are never a donor.
    [GeneratedRegex(@"^(bidrag|beløb|tilskud|donation\w*|cvr\b.*|i alt|total|sum|navn|adresse|modtager|modtaget fra)\s*:?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex LabelNameRegex();

    // A Danish postal code followed by a capitalised town: "1790 København V", "8362 Hørning".
    [GeneratedRegex(@"\b[1-9]\d{3}\s+\p{Lu}")]
    private static partial Regex PostalCodeRegex();

    // "Vester Voldgade 113", "Boulevard 18," — a street name followed by a house number (not a year).
    [GeneratedRegex(@"\p{L}{3,}\.?\s\d{1,3}[a-zA-Z]?(?=[,.\s]|$)")]
    private static partial Regex StreetNumberRegex();

    // An amount at the end of the line: "500.000 kr.", "14.282", "1.200.000,00 kr", "600.585 *)" — never a bare year.
    [GeneratedRegex(@"(?<![\w,.])(?<amount>\d{1,3}(?:\.\d{3})+(?:,\d{1,2})?|\d{5,}(?:,\d{1,2})?|\d{1,4}(?:,\d{1,2})?(?=\s*(?:kr\.?|DKK)))\s*(?:kr\.?|DKK)?\s*(?:\*\)?)?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingAmountRegex();

    // "Penneo dokumentnøgle: …", "This document has signatur…", "Agreement-ID …", stray "document"/"signatur"/"has" column text.
    [GeneratedRegex(@"\s*(penneo dokumentn\w+.*|this document.*|dette dokument.*|delle dokument.*|agreement-id.*)$|\s{2,}(document|signatur|has)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex SignatureJunkRegex();

    // "… København V (vederlagsfri" — an opening parenthesis whose text wrapped to the next line.
    [GeneratedRegex(@"\s*\([^)]*$")]
    private static partial Regex DanglingParenthesisRegex();

    // A line that is only a postal code and town, continuing the previous row's address.
    [GeneratedRegex(@"^[1-9]\d{3}\s+\p{Lu}[\p{L}. ]{1,30}$")]
    private static partial Regex PostalOnlyRegex();

    // A separate amount line under a vertically laid out donor: "Bidrag: 600.585 kr. inklusive ..."
    [GeneratedRegex(@"^(bidrag|beløb|tilskud)\s*:?\s*(?<amount>\d{1,3}(?:\.\d{3})+(?:,\d{1,2})?)\s*kr", RegexOptions.IgnoreCase)]
    private static partial Regex AmountLineRegex();
}
