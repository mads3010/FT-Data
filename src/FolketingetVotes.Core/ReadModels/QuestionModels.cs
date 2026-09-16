namespace FolketingetVotes.Core.ReadModels;

public sealed record QuestionFilter(string? Query = null, int? PeriodId = null, int? AskerId = null, int? MinisterId = null, string? MinisterTitle = null);

/// <summary>A § 20 question from a member to a minister.</summary>
public sealed record QuestionListItem(
    int CaseId,
    string? Number,
    string Title,
    int? AskerId,
    string? AskerName,
    string? AskerParty,
    string? MinisterTitle,
    int? MinisterPersonId,
    string? MinisterPersonName,
    DateTime? AskedDate,
    DateTime? AnsweredDate,
    bool Oral,
    bool Withdrawn,
    string PeriodTitle,
    string PeriodCode)
{
    public int? DaysToAnswer => AskedDate is { } a && AnsweredDate is { } b ? (int)(b.Date - a.Date).TotalDays : null;

    /// <summary>Folketinget's page for the question (ft.dk).</summary>
    public string? FolketingetUrl => ExternalLinks.QuestionUrl(PeriodCode, Number);
}

public sealed record QuestionStats(int Total, int Answered, int Oral, int Withdrawn, double? MedianDaysToAnswer);

public sealed record MinisterQuestionRow(string MinisterTitle, int? MinisterPersonId, string? MinisterPersonName, int Questions, double? MedianDaysToAnswer);
