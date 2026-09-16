namespace FolketingetVotes.Core.Entities;

/// <summary>
/// A cached, machine-generated summary of a case. Not populated in v1; the table and
/// <see cref="Abstractions.ISummaryProvider"/> exist so an AI summariser can be added without schema changes.
/// </summary>
public sealed class BillSummary
{
    public int CaseId { get; set; }
    public required string Provider { get; set; }
    public required string Model { get; set; }
    public required string PromptVersion { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? SourceUrl { get; set; }

    /// <summary>JSON-serialised <see cref="Abstractions.BillSummaryContent"/>.</summary>
    public required string ContentJson { get; set; }
}
