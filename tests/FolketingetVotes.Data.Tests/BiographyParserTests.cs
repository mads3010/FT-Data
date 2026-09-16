using FolketingetVotes.Core;
using FolketingetVotes.Data.Oda;

namespace FolketingetVotes.Data.Tests;

public class BiographyParserTests
{
    private const string Biography = """
        <member><constituencies>
        <constituency>Folketingsmedlem for Moderaterne i Sjællands Storkreds fra 19. maj 2022. </constituency>
        <constituency>Folketingsmedlem for Venstre i Nordsjællands Storkreds, 13. november 2007 – 18. juni 2015. </constituency>
        <constituency>Folketingsmedlem for Det Konservative Folkeparti i Københavns Amtskreds 14. maj 1957 - 21. november 1966.</constituency>
        <constituency>Folketingsmedlem for Uden for folketingsgrupperne i Sjællands Storkreds, 1. januar 2021 – 18. maj 2022. </constituency>
        <constituency>Midlertidigt folketingsmedlem for Socialdemokratiet i Fyns Storkreds, 3. marts 2019 – 30. april 2019.</constituency>
        <constituency>Noget helt andet uden dato.</constituency>
        </constituencies></member>
        """;

    [Fact]
    public void Parses_open_closed_and_hyphenated_terms()
    {
        var terms = BiographyParser.ParseMemberships(145, Biography);

        Assert.Equal(5, terms.Count);
        Assert.All(terms, t => Assert.Equal(145, t.PersonId));

        Assert.Equal(("M", new DateOnly(2022, 5, 19), (DateOnly?)null, "Sjællands Storkreds"), (terms[0].PartyShortName, terms[0].StartDate, terms[0].EndDate, terms[0].Constituency));
        Assert.Equal(("V", new DateOnly(2007, 11, 13), (DateOnly?)new DateOnly(2015, 6, 18)), (terms[1].PartyShortName, terms[1].StartDate, terms[1].EndDate));
        Assert.Equal(("KF", new DateOnly(1957, 5, 14), (DateOnly?)new DateOnly(1966, 11, 21)), (terms[2].PartyShortName, terms[2].StartDate, terms[2].EndDate));
        Assert.Equal("UFG", terms[3].PartyShortName);
        Assert.Equal("Uden for folketingsgrupperne", terms[3].PartyName);
        Assert.Equal(("S", new DateOnly(2019, 3, 3)), (terms[4].PartyShortName, terms[4].StartDate));
        Assert.True(terms[4].IsTemporary);
        Assert.False(terms[0].IsTemporary);
    }

    [Fact]
    public void Empty_or_missing_biography_yields_nothing()
    {
        Assert.Empty(BiographyParser.ParseMemberships(1, null));
        Assert.Empty(BiographyParser.ParseMemberships(1, "<member><career/></member>"));
    }

    [Theory]
    [InlineData("15. september 2011", 2011, 9, 15)]
    [InlineData("1. januar 2021", 2021, 1, 1)]
    [InlineData("31. december 1999", 1999, 12, 31)]
    public void Parses_danish_dates(string text, int y, int m, int d)
        => Assert.Equal(new DateOnly(y, m, d), BiographyParser.ParseDate(text));

    [Theory]
    [InlineData("30. februar 2020")]
    [InlineData("5. smarch 2020")]
    [InlineData("no date")]
    public void Rejects_invalid_dates(string text) => Assert.Null(BiographyParser.ParseDate(text));

    [Theory]
    [InlineData("Venstre", "V")]
    [InlineData("Radikale Venstre", "RV")]
    [InlineData("Det Radikale Venstre", "RV")]
    [InlineData("Venstre, Danmarks Liberale Parti", "V")]
    [InlineData("Danmarksdemokraterne – Inger Støjberg", "DD")]
    [InlineData("socialdemokratiet", "S")]
    [InlineData("Uden for folketingsgrupperne", "UFG")]
    [InlineData("Javnaðarflokkurin", "JF")]
    [InlineData("Ukendt Parti", null)]
    [InlineData("", null)]
    public void Party_names_map_to_short_names(string name, string? expected) => Assert.Equal(expected, PartyNames.ShortNameFor(name));
}
