using FolketingetVotes.Data.PartyAccounts;

namespace FolketingetVotes.Data.Tests;

public class PartyAccountParserTests
{
    private static readonly PdfPageText[] SamplePages =
    [
        new(1, "De politiske partiers regnskaber for 2023\nIndhold\n"),
        new(4, """
            Socialdemokratiet
            Årsregnskab 2023
            Resultatopgørelse
            Offentlig partistøtte 45.000.000 kr.
            Bidrag over 20.000 kr. fra private bidragydere:
            Fagligt Fælles Forbund, Kampmannsgade 4, 1790 København V 500.000 kr.
            Dansk Metal, Molestien 7, 2450 København SV 250.000,00 kr.
            HK Danmark 100.000
            Anonyme bidrag: ingen
            Balance
            """),
        new(9, """
            Venstre, Danmarks Liberale Parti
            Tilskud, der overstiger 20.000 kr.:
            Der er ikke modtaget bidrag over grænsen.
            Ledelsespåtegning
            """),
        new(12, """
            Liberal Alliance
            Årsrapport
            Private bidragydere, som har givet mere end 20.000 kr. i regnskabsåret
            Saxo Bank A/S, Philip Heymans Allé 15, 2900 Hellerup 1.200.000 kr.
            Anonyme bidrag under grænsen 12.000 kr.
            """),
    ];

    [Fact]
    public void Splits_document_into_party_sections()
    {
        var accounts = PartyAccountParser.Parse(SamplePages);
        Assert.Equal(["Socialdemokratiet", "Venstre, Danmarks Liberale Parti", "Liberal Alliance"], accounts.Select(a => a.PartyName));
        Assert.Equal([4, 9, 12], accounts.Select(a => a.FirstPage));
    }

    [Fact]
    public void Extracts_named_donors_with_amounts_and_addresses()
    {
        var s = PartyAccountParser.Parse(SamplePages).Single(a => a.PartyName == "Socialdemokratiet");
        Assert.Equal(3, s.Donations.Count);

        var first = s.Donations[0];
        Assert.Equal("Fagligt Fælles Forbund", first.DonorName);
        Assert.Equal("Kampmannsgade 4, 1790 København V", first.DonorAddress);
        Assert.Equal(500_000m, first.Amount);
        Assert.Equal(4, first.PageNumber);
        Assert.Contains("500.000 kr.", first.RawText, StringComparison.Ordinal);

        Assert.Equal(250_000m, s.Donations[1].Amount);
        Assert.Equal("HK Danmark", s.Donations[2].DonorName);
        Assert.Equal(100_000m, s.Donations[2].Amount);
        Assert.Null(s.Donations[2].DonorAddress);
    }

    [Fact]
    public void Stops_at_block_end_and_ignores_none_statements()
    {
        var accounts = PartyAccountParser.Parse(SamplePages);
        Assert.Empty(accounts.Single(a => a.PartyName.StartsWith("Venstre", StringComparison.Ordinal)).Donations);
        var la = accounts.Single(a => a.PartyName == "Liberal Alliance");
        Assert.Single(la.Donations);
        Assert.Equal("Saxo Bank A/S", la.Donations[0].DonorName);
        Assert.Equal(1_200_000m, la.Donations[0].Amount);
    }

    [Theory]
    [InlineData("Socialdemokratiet", "S")]
    [InlineData("Venstre, Danmarks Liberale Parti", "V")]
    [InlineData("Det Konservative Folkeparti", "KF")]
    [InlineData("Ukendt Parti", null)]
    public void Maps_party_names_to_short_names(string name, string? expected)
        => Assert.Equal(expected, PartyAccountParser.ShortNameFor(name));

    [Theory]
    [InlineData("partiregnskaber_2018.pdf", 2018)]
    [InlineData("Partiernes regnskaber 2023 (samlet).pdf", 2023)]
    [InlineData("regnskab.pdf", null)]
    public void Reads_year_from_file_name(string file, int? expected)
        => Assert.Equal(expected, PartyAccountImporter.YearFromFileName(file));
}
