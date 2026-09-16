using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace FolketingetVotes.Data.PartyAccounts;

/// <summary>Extracts page text in reading order from a PDF using PdfPig (pure .NET, no native dependencies).</summary>
public static class PartyAccountPdfReader
{
    public static IReadOnlyList<PdfPageText> Read(string path)
    {
        using var document = PdfDocument.Open(path);
        var pages = new List<PdfPageText>(document.NumberOfPages);
        foreach (var page in document.GetPages())
        {
            pages.Add(new PdfPageText(page.Number, ContentOrderTextExtractor.GetText(page, addDoubleNewline: false)));
        }

        return pages;
    }
}
