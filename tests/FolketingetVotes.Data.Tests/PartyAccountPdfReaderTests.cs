using FolketingetVotes.Data.PartyAccounts;

namespace FolketingetVotes.Data.Tests;

public class PartyAccountPdfReaderTests
{
    [Fact]
    public void Parses_ocr_sidecar_into_pages()
    {
        const string sidecar = "=== Page 1 ===\nForside\n=== Page 2 ===\n\n=== Page 3 ===\nSocialdemokratiet\nBidrag over 20.000 kr.\n";
        var pages = PartyAccountPdfReader.ParseSidecar(sidecar);

        Assert.Equal([1, 2, 3], pages.Select(p => p.PageNumber));
        Assert.Equal("Forside\n", pages[0].Text);
        Assert.Equal("\n", pages[1].Text);
        Assert.Contains("Bidrag over 20.000 kr.", pages[2].Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Sidecar_path_sits_next_to_the_pdf()
        => Assert.Equal("/x/Partiregnskaber 2019.ocr.txt", PartyAccountPdfReader.SidecarPathFor("/x/Partiregnskaber 2019.pdf"));

    [Fact]
    public void Reads_txt_input_directly()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        File.WriteAllText(path, "=== Page 7 ===\nVenstre\n");
        try
        {
            var pages = PartyAccountPdfReader.Read(path);
            Assert.Single(pages);
            Assert.Equal(7, pages[0].PageNumber);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
