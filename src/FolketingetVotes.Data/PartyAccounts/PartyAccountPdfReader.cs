using System.Globalization;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace FolketingetVotes.Data.PartyAccounts;

/// <summary>
/// Reads page text for a party-accounts file. Folketinget's PDFs are usually scans without a text layer, so the
/// reader prefers an OCR sidecar (<c>file.ocr.txt</c>, produced by <c>tools/ocr-pdf</c>) when present, accepts a
/// <c>.txt</c> path directly, and otherwise extracts embedded text with PdfPig.
/// </summary>
public static partial class PartyAccountPdfReader
{
    /// <summary>Below this many characters per page on average the PDF is treated as a scan.</summary>
    private const int MinimumCharactersPerPage = 40;

    public static IReadOnlyList<PdfPageText> Read(string path)
    {
        if (path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return ParseSidecar(File.ReadAllText(path));
        }

        var sidecar = SidecarPathFor(path);
        if (File.Exists(sidecar))
        {
            return ParseSidecar(File.ReadAllText(sidecar));
        }

        var pages = ReadEmbeddedText(path);
        var averageCharacters = pages.Count == 0 ? 0 : pages.Sum(p => p.Text.Length) / pages.Count;
        if (averageCharacters < MinimumCharactersPerPage)
        {
            throw new InvalidOperationException(
                $"'{Path.GetFileName(path)}' has no usable text layer (average {averageCharacters} characters per page). " +
                $"OCR it first, e.g. tools/ocr-pdf/ocr-pdf \"{path}\", which writes {Path.GetFileName(sidecar)} next to it.");
        }

        return pages;
    }

    public static string SidecarPathFor(string pdfPath) => Path.ChangeExtension(pdfPath, ".ocr.txt");

    /// <summary>Parses the sidecar format: "=== Page N ===" markers followed by that page's lines.</summary>
    public static IReadOnlyList<PdfPageText> ParseSidecar(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var pages = new List<PdfPageText>();
        var current = new System.Text.StringBuilder();
        var pageNumber = 0;
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var marker = PageMarkerRegex().Match(line);
            if (marker.Success)
            {
                if (pageNumber > 0)
                {
                    pages.Add(new PdfPageText(pageNumber, current.ToString()));
                }

                pageNumber = int.Parse(marker.Groups[1].Value, CultureInfo.InvariantCulture);
                current.Clear();
                continue;
            }

            if (pageNumber > 0)
            {
                current.Append(line).Append('\n');
            }
        }

        if (pageNumber > 0)
        {
            pages.Add(new PdfPageText(pageNumber, current.ToString()));
        }

        return pages;
    }

    private static List<PdfPageText> ReadEmbeddedText(string path)
    {
        using var document = PdfDocument.Open(path);
        var pages = new List<PdfPageText>(document.NumberOfPages);
        foreach (var page in document.GetPages())
        {
            pages.Add(new PdfPageText(page.Number, ContentOrderTextExtractor.GetText(page, addDoubleNewline: false)));
        }

        return pages;
    }

    [GeneratedRegex(@"^=== Page (\d+) ===$")]
    private static partial Regex PageMarkerRegex();
}
