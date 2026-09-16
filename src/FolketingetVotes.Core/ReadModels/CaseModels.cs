using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Core.ReadModels;

public sealed record CaseSummary(
    int CaseId,
    CaseType Type,
    string Title,
    string? ShortTitle,
    string? Number,
    int StatusId,
    string? StatusName,
    int PeriodId,
    string PeriodCode,
    string PeriodTitle);

public sealed record CaseActorRow(int ActorId, string Name, int RoleId, string RoleName, ActorType ActorType);

public sealed record CaseStepRow(int StepId, string Title, DateTime? Date, string? TypeName, string? StatusName);

public sealed record KeywordRow(int KeywordId, string Name, int TypeId);

public sealed record CaseFilter(string? Query = null, int? PeriodId = null, CaseType? Type = null, bool OnlyWithVotes = false);

public sealed record CaseDetail(
    CaseSummary Case,
    string? Summary,
    string? VotingConclusion,
    int? LawNumber,
    DateTime? LawDate,
    string? RetsinformationUrl,
    IReadOnlyList<CaseActorRow> Actors,
    IReadOnlyList<CaseStepRow> Steps,
    IReadOnlyList<VoteListItem> Votes,
    IReadOnlyList<KeywordRow> Keywords)
{
    /// <summary>Public case page on folketingstidende.dk, which serves bill texts without bot challenges.</summary>
    public string? FolketingstidendeUrl => ExternalLinks.FolketingstidendeCaseUrl(Case.PeriodCode, Case.Type, Case.Number);

    /// <summary>PDF of the bill as introduced ("som fremsat"), when the case is a bill or resolution.</summary>
    public string? BillTextPdfUrl => ExternalLinks.BillAsIntroducedPdfUrl(Case.PeriodCode, Case.Type, Case.Number);
}

/// <summary>Deterministic links to Folketinget's public document sites, built from the case number and session code.</summary>
public static class ExternalLinks
{
    private const string FolketingstidendeBase = "https://www.folketingstidende.dk";

    public static string? FolketingstidendeCaseUrl(string periodCode, CaseType type, string? number)
    {
        var (segment, slug) = Segment(type, number);
        return segment is null ? null : $"{FolketingstidendeBase}/samling/{periodCode}/{segment}/{slug}/index.htm";
    }

    /// <summary>Transcript of a chamber sitting: /samling/20231/salen/M10/20231_M10_referat.pdf (verified to resolve).</summary>
    public static string? TranscriptPdfUrl(string periodCode, string? meetingNumber)
    {
        if (string.IsNullOrWhiteSpace(periodCode) || string.IsNullOrWhiteSpace(meetingNumber) || !meetingNumber.All(char.IsDigit))
        {
            return null;
        }

        return $"{FolketingstidendeBase}/samling/{periodCode}/salen/M{meetingNumber}/{periodCode}_M{meetingNumber}_referat.pdf";
    }

    public static string? BillAsIntroducedPdfUrl(string periodCode, CaseType type, string? number)
    {
        var (segment, slug) = Segment(type, number);
        if (segment is null || slug is null)
        {
            return null;
        }

        var lower = slug.ToLowerInvariant();
        return $"{FolketingstidendeBase}/ripdf/samling/{periodCode}/{segment}/{lower}/{periodCode}_{lower}_som_fremsat.pdf";
    }

    private static (string? Segment, string? Slug) Segment(CaseType type, string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            return (null, null);
        }

        var slug = number.Replace(" ", string.Empty, StringComparison.Ordinal);
        return type switch
        {
            CaseType.Bill => ("lovforslag", slug),
            CaseType.Resolution => ("beslutningsforslag", slug),
            _ => (null, null),
        };
    }
}
