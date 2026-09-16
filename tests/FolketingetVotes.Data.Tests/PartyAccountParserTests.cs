using FolketingetVotes.Data.PartyAccounts;

namespace FolketingetVotes.Data.Tests;

/// <summary>Fixtures are excerpts of the OCR text of Folketinget's 2023 party accounts (including OCR errors).</summary>
public class PartyAccountParserTests
{
    private static readonly PdfPageText[] SamplePages =
    [
        new(1, "FOLKETINGET\nDe politiske partiers regnskaber for 2023\n"),
        new(16, """
            Socialdemokratiet
            Noter
            2023  2022
            Note I. Tilskud, partistøtte og andre indtægter
            Offentlig partistette  34.748.821  31.792.149
            Anonyme tilskud i alt  0
            Al nedenstående bidragydere har Socialdemokratiet fået stiller modefacilteter til rådighed ill en
            samlet værdi af ca. 2001. Ar.
            3F  Kampmannsgade 4  1790 København V
            Følgende organisationer / virksomheder har indbetalt et beloh, der er storre end 22,8 t.kr.
            Arbejderes Landsbank  Vesterbrogade 5  1502 København V
            Folgende enkeltpersoner har indbetalt i partiskat et betob, der er storre end 22,8 c.kr.
            Ane Halsboe-Jørgensen  Folketinget, Christiansborg  1240 København K
            Folketinget, Christiansborg  1240 København K
            Mette Frederiksen  Folketinget, Christiansborg  1240 Kobenhavn K
            12
            """),
        new(17, """
            Peter Hummelgaard  Folketinget, Christiansborg  1240 København K
            13
            """),
        new(32, """
            Venstres Landsorganisation
            Noter
            1. Private bidrag
            Under henvisning til § 3 i lovbekendtgørelse nr. 139 af 7. februar 2019 om private bidrag til politiske parti-
            oplyses, at følgende personer, organsationer og virksomheder har givet bidrag og/eller stillet faciliteter til
            rådighed for et beløb på over 22.800 kr., if. § 1. (2023-niveau):
            Dansk Arbejdsgiverforening, Vester Voldgade 113, 1552 København V
            ••
            Dansk Industri, H.C. Andersens Boulevard 18, 1553 København V
            Den Liberale Erhvervsklub, Søllerødvej 30, 2840 Holte*
            *) Bidrag til Den Liberale Erhvervsklub og Den Liberale Erhvervsforening, der overstiger 22.800 kr., opfø-
            res selvstændigt på ovenstående liste.
            2. Begivenheder efter balancedagen
            Venstres Landsorganisation har efter balancedagen solgt domicilejendommen beliggende Søllerødvej 30,
            """),
        new(40, """
            Moderaterne
            Resultatopgørelse
            Donationer  2  230.800
            Donationer over 22.800 kr  279.192
            """),
        new(43, """
            Moderaterne
            Noter til årsregnskabet
            2 Donationer over 22.800 kr
            Følgende personer, organisationer og virksomheder har givet bidrag eller stillet faciliteter til rådighed
            for beløb over 22.800 kr.:
            Dansk Generationsskifte A/S  Filippavej 57, 5762 Vester Skerninge
            Dansk Industri  H. C. Andersens Boulevard 18, København V
            3 Indberetningspligtige filskud til kandidater
            Indberetningspligtige tilskud til kandidater, der er opstillet for Moderaterne til folketingsvalg og
            """),
        new(67, """
            Bilag til Indenrigsministeriet og Sundhedsministeriet
            regnskabet for Socialistisk Folkeparti således:
            Oplysninger om private tilskud, som overstiger kr. 22.800:
            De personer og organisationer, som har ydet tilskud med mere end kr. 22.800 fremgår af nedenstående
            oversigt:
            Private personer:  signatur
            Navn  Adresse  has
            Lisbeth Bech-Nielsen  Christiansborg. 1240 København K.
            Marianne Bigum  Korsgade 56, 1, 2200 Kobenhavn N  document
            20
            """),
        new(68, """
            Oplysninger om private tilskud, som overstiger kr. 22.800:
            (fortsat)
            Organisationer:
            Navn  Adresse
            HK  Weidekampsgade 8, 2300 København S
            Der er modtaget kr. O i anonyme beløb i 2023, og der er ikke retureret anonyme beløb til tilskudsgivere
            """),
        new(85, """
            Danmarksdemokraterne • Årsregnskab for 2023
            Noter
            1.  Bidrag fra private
            adresse på tilskudsydere, der i regnskabsåret har ydet tilskudsbidrag af en værdi på mere end kr.
            22.800 jf. § 1 i bekendtgørelse om regulering af beløb i partiregnskabsloven og partistøtteloven i
            Tilskudsyder:  Adresse:
            Danmarksdemokraternes Erhvervs-  Christiansborg Slotsplads 1, 1218 København K
            politiske Forening
            Danmarksdemokraterne skal jf. § 3, stk. 2, pkt. 2 i lov om private bidrag til politiske partier og
            """),
        new(121, """
            Det Konservative Folkeparti
            Noter
            Opgørelse af frivillige bidrag over 22.800 kr., j/. LBK nr. 139 af 7. februar 2019 iht. § 3.
            Dansk Arbejdsgiverforening, Vester Voldgade 113, 1790 København V
            • Foreningen C-Business, C/O Det Konservative Folkeparti, Christiansborg Slot 1, 1118 København K
            De private og erhvervsmæssige bidrag har været anvendt i forbindelse med politiske aktiviteter i 2023.
            Anonyme bidrag
            """),
        new(136, """
            Enhedslisten
            NOTER TIL ÅRSREGNSKABET
            Enhedslisten har frivilligt valgt at oplyse modtagne tilskud på over 5.000 kr
            Tilskud (partiskat) over 5.000 kr. er i 2023 modtaget fra folende bidragsydere  kr.
            Årsrapport 2023
            Christian Juhl  Bindslevs Plads 12  8600 Silkeborg  113.033  Penneo dokumentnøgle: 46MXB-1EYLE
            Per Clausen  Vestre Fjordvej 34, 3. tv  9000 Aalborg  68.000
            Frank Aaen  Jagtvej 197, 2. th  2100 København Ø  33.657
            Tilskud over 5.000 kr. er i 2023 modtaget fra Enhedslistens repræsentanter i Danske Regioner
            (midlerne er videregivet til Enhedslistens regionsgruppe)
            3250 Gilleleje  8.100
            Tilskud over 5.000 kr. er i 2023 mouraget fra følgende private bidragsydere
            Arv fra boet efter Ib Sønderkær  189.671
            Vedr. indberetningspligtige tilskud til Kandidater:
            """),
        new(149, """
            Liberal Alliance Landsorganisation
            tagne tilskud fra private tilskudsydere over 22.800 kr.
            Landsforbundet har i 2023 modtaget beløb og naturalieydelser fra enkeltdonorer på over 22.800 kr. fra
            følgende private, virksomheder, fonde og organisationer:
            Dansk Arbejdsgivertorening
            Vester Voldgade 113. 1552 København V
            Bidrag: 600.585 kr. inklusive værdi af benyttelse af mødefaciliteter.
            Dansk Industri
            H.C. Andersens Blvd. 18, 1553 København K
            Bidrag: 82.609 kr. inklusive værdi af benyttelse af mødefaciliteter.
            Personaleomkostninger
            """),
        new(150, """
            Radikale Venstres Landsforbund
            Ledelsesberetning
            Lokale foreningsled og kandidater har oplyst, at de i kalenderåret 2023 har modtaget donationer over
            beløbsgrænsen på 22.800 kr. som følger:
            Modtager: Hovedstadens Radikale Venstre med beslutning om kampagneaktiviteter til støtte for Sa-
            mira Nawa
            Beløb: 75.000 kr.
            Modtaget fra:
            Kristjan Wager
            Mysundegade 7, 2. th
            1668 Kbh V
            Oplysninger om donationer til lokale foreningsled og kandidater er baseret på indrapporterede oplysnin-
            """),
        new(206, """
            Dansk Folkeparti Landsorganisation
            Noter til årsregnskabet
            1 Følgende personer, organisationer og virksomheder har givet bidrag for beløt over 22.800 kr.:
            Der har ikke varet donationer for over 22.800 kr. i regnskabsåret.
            2023  2022
            Andre private tilskud fra private personer, under 22.800 kr.  106.109  103.603
            """),
        new(253, """
            Folkebevægelsen mod EU
            Noter
            3  Frivillige bidrag
            Under henvisning til § 3 i lov om private bidrag til politiske partier og offentliggørelse af politiske
            partiers regnskaber skal oplyses, at føigende tilskudsydere har givet bidrag for et beløb over kr.
            20.000.
            - Enhedslisten, Folketinget, 1429 København K
            - Birte Uhre Pedersen, Burmeistergade 30, 2. tv., 1429 Købehavn K
            Der er ikke returneret beløb til tilskudsgiver eller overført beløb til Økonomi- og Indenrigsministeriet i
            """),
    ];

    private static IReadOnlyList<ParsedPartyAccount> Parsed => PartyAccountParser.Parse(SamplePages);

    private static ParsedPartyAccount Section(string name) => Parsed.Single(a => a.PartyName == name);

    [Fact]
    public void Splits_document_into_party_sections_in_order()
    {
        Assert.Equal(
            ["Socialdemokratiet", "Venstres Landsorganisation", "Moderaterne", "Socialistisk Folkeparti", "Danmarksdemokraterne", "Det Konservative Folkeparti", "Enhedslisten", "Liberal Alliance Landsorganisation", "Radikale Venstres Landsforbund", "Dansk Folkeparti Landsorganisation", "Folkebevægelsen mod EU"],
            Parsed.Select(a => a.PartyName));
        Assert.Equal(16, Section("Socialdemokratiet").FirstPage);
    }

    [Fact]
    public void Two_space_columns_with_lists_continuing_on_the_next_page()
    {
        var s = Section("Socialdemokratiet").Donations;
        Assert.Equal(["3F", "Arbejderes Landsbank", "Ane Halsboe-Jørgensen", PartyAccountParser.UnreadableName, "Mette Frederiksen", "Peter Hummelgaard"], s.Select(d => d.DonorName));
        Assert.Equal("Kampmannsgade 4, 1790 København V", s[0].DonorAddress);
        Assert.Equal(17, s[5].PageNumber);
        Assert.All(s, d => Assert.Null(d.Amount));
        Assert.Contains("indbetalt i partiskat", s[4].Note, StringComparison.Ordinal);
    }

    [Fact]
    public void Comma_rows_with_footnote_stars_and_ocr_noise()
    {
        var v = Section("Venstres Landsorganisation").Donations;
        Assert.Equal(["Dansk Arbejdsgiverforening", "Dansk Industri", "Den Liberale Erhvervsklub"], v.Select(d => d.DonorName));
        Assert.Equal("Søllerødvej 30, 2840 Holte", v[2].DonorAddress);
    }

    [Fact]
    public void Income_statement_lines_are_not_headers_but_note_titles_are()
    {
        var m = Section("Moderaterne").Donations;
        Assert.Equal(["Dansk Generationsskifte A/S", "Dansk Industri"], m.Select(d => d.DonorName));
        Assert.Equal("H. C. Andersens Boulevard 18, København V", m[1].DonorAddress);
    }

    [Fact]
    public void Sub_headings_and_column_headers_are_skipped()
    {
        var sf = Section("Socialistisk Folkeparti").Donations;
        Assert.Equal(["Lisbeth Bech-Nielsen", "Marianne Bigum", "HK"], sf.Select(d => d.DonorName));
        Assert.Equal("Weidekampsgade 8, 2300 København S", sf[2].DonorAddress);
    }

    [Fact]
    public void Wrapped_names_are_joined()
    {
        var dd = Section("Danmarksdemokraterne").Donations;
        var donor = Assert.Single(dd);
        Assert.Equal("Danmarksdemokraternes Erhvervspolitiske Forening", donor.DonorName);
        Assert.Equal("Christiansborg Slotsplads 1, 1218 København K", donor.DonorAddress);
    }

    [Fact]
    public void Bulleted_rows_and_block_end()
    {
        var kf = Section("Det Konservative Folkeparti").Donations;
        Assert.Equal(["Dansk Arbejdsgiverforening", "Foreningen C-Business"], kf.Select(d => d.DonorName));
    }

    [Fact]
    public void Name_variants_merge_into_one_account_and_junk_is_stripped()
    {
        PdfPageText[] pages =
        [
            new(3, "Socialdemokratiet\nÅrsregnskab 2023\n"),
            new(5, "Socialdemokratiet i Danmark\nPartioplysninger\nBidrag over 22.800 kr.:\nSelskab for Liberale Visioner, Christiansborg Slot 1, 1218 København K  Penneo dokumentnøgle: 2AAES-LEEXE\nDansk Arbejdsgiverforening, Vester Voldgade 113, 1552 København V (vederlagsfri\nÅrsrapport 2023\n"),
            new(9, "Noter\nBidrag\nBalance\nFolkebevægelsen mod EU  13\n"),
        ];
        var accounts = PartyAccountParser.Parse(pages);
        Assert.Equal(["Socialdemokratiet", "Folkebevægelsen mod EU"], accounts.Select(a => a.PartyName));
        var s = accounts[0];
        Assert.Equal(["Selskab for Liberale Visioner", "Dansk Arbejdsgiverforening"], s.Donations.Select(d => d.DonorName));
        Assert.Equal("Vester Voldgade 113, 1552 København V", s.Donations[1].DonorAddress);
    }

    [Fact]
    public void A_row_whose_name_was_lost_reuses_the_previous_address()
    {
        var s = Section("Socialdemokratiet").Donations;
        var lost = s.Single(d => d.DonorName == PartyAccountParser.UnreadableName);
        Assert.Equal("Folketinget, Christiansborg, 1240 København K", lost.DonorAddress);
        Assert.DoesNotContain(s, d => d.DonorName == "Folketinget, Christiansborg");
    }

    [Fact]
    public void Amounts_and_missing_names()
    {
        var el = Section("Enhedslisten").Donations;
        Assert.Equal(5, el.Count);
        Assert.Equal(("Christian Juhl", 113_033m, "Bindslevs Plads 12, 8600 Silkeborg"), (el[0].DonorName, el[0].Amount, el[0].DonorAddress));
        Assert.Equal(("Per Clausen", 68_000m), (el[1].DonorName, el[1].Amount));
        Assert.Equal("Vestre Fjordvej 34, 3. tv, 9000 Aalborg", el[1].DonorAddress);
        Assert.Equal((PartyAccountParser.UnreadableName, "3250 Gilleleje", 8_100m), (el[3].DonorName, el[3].DonorAddress, el[3].Amount));
        Assert.Equal(("Arv fra boet efter Ib Sønderkær", 189_671m), (el[4].DonorName, el[4].Amount));
        Assert.Contains("over 5.000 kr", el[0].Note, StringComparison.Ordinal);
    }

    [Fact]
    public void Vertical_layout_with_separate_amount_lines()
    {
        var la = Section("Liberal Alliance Landsorganisation").Donations;
        Assert.Equal(2, la.Count);
        Assert.Equal(("Dansk Arbejdsgivertorening", "Vester Voldgade 113. 1552 København V", 600_585m), (la[0].DonorName, la[0].DonorAddress, la[0].Amount));
        Assert.Equal(("Dansk Industri", 82_609m), (la[1].DonorName, la[1].Amount));
    }

    [Fact]
    public void Amount_stated_before_a_vertically_listed_donor()
    {
        var rv = Section("Radikale Venstres Landsforbund").Donations;
        var donor = Assert.Single(rv);
        Assert.Equal(("Kristjan Wager", "Mysundegade 7, 2. th, 1668 Kbh V", 75_000m), (donor.DonorName, donor.DonorAddress, donor.Amount));
    }

    [Fact]
    public void Labels_cvr_numbers_and_repeated_listings_are_not_rows()
    {
        PdfPageText[] pages =
        [
            new(50, "Venstre\nLedelsesberetning\nFølgende har givet bidrag over 22.400 kr.:\nCVR nr.: 31824559\nDansk Industri  H.C. Andersens Blvd. 18, 1553 København K  4.471.925\nBidrag 850.000 kr.\nAnonyme bidrag\n"),
            new(60, "Venstre\nNoter\nBidrag over 22.400 kr.:\n● Dansk Industri  H. C. Andersens Blvd. 18, 1553 København  4.471.925\nAnonyme bidrag\n"),
        ];
        var v = Assert.Single(PartyAccountParser.Parse(pages));
        var donor = Assert.Single(v.Donations);
        Assert.Equal(("Dansk Industri", 4_471_925m, 50), (donor.DonorName, donor.Amount, donor.PageNumber));
    }

    [Fact]
    public void Ledger_lines_naming_another_organisation_do_not_switch_section()
    {
        PdfPageText[] pages =
        [
            new(96, "Enhedslisten\nÅrsrapport 2022\n"),
            new(108, "Resultatopgørelse\nValg og folkeafstemninger  4.389.893  843.358\nTilskud til Folkebevægelsen mod EU  1.000.000  1.000.000\nNoter\nTilskud over 5.000 kr. er i 2022 modtaget fra følgende bidragsydere\nPer Clausen  Vestre Fjordvej 34  9000 Aalborg  68.000\nVedr. indberetningspligtige tilskud\n"),
        ];
        var account = Assert.Single(PartyAccountParser.Parse(pages));
        Assert.Equal("Enhedslisten", account.PartyName);
        Assert.Single(account.Donations);
    }

    [Fact]
    public void None_statements_yield_no_rows()
    {
        Assert.Empty(Section("Dansk Folkeparti Landsorganisation").Donations);
    }

    [Fact]
    public void Dash_rows_with_threshold_on_the_next_line()
    {
        var f = Section("Folkebevægelsen mod EU").Donations;
        Assert.Equal(["Enhedslisten", "Birte Uhre Pedersen"], f.Select(d => d.DonorName));
        Assert.Equal("Burmeistergade 30, 2. tv., 1429 Købehavn K", f[1].DonorAddress);
        Assert.Null(PartyAccountParser.ShortNameFor("Folkebevægelsen mod EU"));
    }

    [Theory]
    [InlineData("Socialdemokratiet", "S")]
    [InlineData("Venstres Landsorganisation", "V")]
    [InlineData("Det Konservative Folkeparti", "KF")]
    [InlineData("Dansk Folkeparti Landsorganisation", "DF")]
    [InlineData("Ukendt Parti", null)]
    public void Maps_party_names_to_short_names(string name, string? expected)
        => Assert.Equal(expected, PartyAccountParser.ShortNameFor(name));

    [Theory]
    [InlineData("partiregnskaber_2018.pdf", 2018)]
    [InlineData("Partiregnskaber 2019.pdf", 2019)]
    [InlineData("regnskab.pdf", null)]
    public void Reads_year_from_file_name(string file, int? expected)
        => Assert.Equal(expected, PartyAccountImporter.YearFromFileName(file));
}
