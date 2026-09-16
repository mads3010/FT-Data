using System.Globalization;
using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Web.Services;

/// <summary>Danish display text for enums and consistent number/date formatting.</summary>
public static class Labels
{
    private static readonly CultureInfo Da = CultureInfo.GetCultureInfo("da-DK");

    public static string Ballot(BallotType type) => type switch
    {
        BallotType.For => "For",
        BallotType.Against => "Imod",
        BallotType.Abstain => "Hverken for eller imod",
        BallotType.Absent => "Fraværende",
        _ => type.ToString(),
    };

    public static string BallotShort(BallotType type) => type switch
    {
        BallotType.For => "For",
        BallotType.Against => "Imod",
        BallotType.Abstain => "Hverken",
        BallotType.Absent => "Fravær",
        _ => type.ToString(),
    };

    public static string BallotCss(BallotType type) => type switch
    {
        BallotType.For => "for",
        BallotType.Against => "against",
        BallotType.Abstain => "abstain",
        BallotType.Absent => "absent",
        _ => "absent",
    };

    public static string VoteType(VoteType type) => type switch
    {
        Core.Enums.VoteType.FinalPassage => "Endelig vedtagelse",
        Core.Enums.VoteType.CommitteeRecommendation => "Udvalgsindstilling",
        Core.Enums.VoteType.MotionForResolution => "Forslag til vedtagelse",
        Core.Enums.VoteType.Amendment => "Ændringsforslag",
        _ => type.ToString(),
    };

    public static string CaseType(CaseType type) => type switch
    {
        Core.Enums.CaseType.Bill => "Lovforslag",
        Core.Enums.CaseType.Resolution => "Beslutningsforslag",
        Core.Enums.CaseType.Interpellation => "Forespørgsel",
        Core.Enums.CaseType.MotionForResolution => "Forslag til vedtagelse",
        Core.Enums.CaseType.Statement => "Redegørelse",
        Core.Enums.CaseType.Appropriation => "Aktstykke",
        Core.Enums.CaseType.GeneralPart => "Alm. del",
        Core.Enums.CaseType.Section20Question => "§ 20-spørgsmål",
        _ => type.ToString(),
    };

    public static string Result(bool passed) => passed ? "Vedtaget" : "Forkastet";

    public static string Date(DateTime value) => value.ToString("d. MMMM yyyy", Da);

    public static string DateShort(DateTime value) => value.ToString("dd.MM.yyyy", Da);

    public static string Date(DateOnly value) => value.ToString("d. MMM yyyy", Da);

    public static string DateTime(DateTime value) => value.ToString("d. MMMM yyyy 'kl.' HH:mm", Da);

    public static string Number(long value) => value.ToString("N0", Da);

    public static string Percent(double? value) => value is null ? "–" : (value.Value * 100).ToString("N0", Da) + " %";

    public static string Money(decimal? value) => value is null ? "beløb ikke angivet" : value.Value.ToString("N0", Da) + " kr.";
}
