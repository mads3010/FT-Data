using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Core.Entities;

/// <summary>Sag: a bill, resolution, interpellation or other parliamentary case.</summary>
public sealed class ParliamentaryCase : IHasId
{
    public int Id { get; set; }
    public CaseType TypeId { get; set; }
    public int? CategoryId { get; set; }
    public int StatusId { get; set; }
    public required string Title { get; set; }
    public string? ShortTitle { get; set; }
    public string? Number { get; set; }
    public string? NumberPrefix { get; set; }
    public int? NumberNumeric { get; set; }
    public string? NumberPostfix { get; set; }

    /// <summary>The official summary ("resume") written by Folketinget's administration.</summary>
    public string? Summary { get; set; }

    /// <summary>The official voting conclusion text ("afstemningskonklusion").</summary>
    public string? VotingConclusion { get; set; }

    public int PeriodId { get; set; }
    public int? LawNumber { get; set; }
    public DateTime? LawDate { get; set; }
    public string? RetsinformationUrl { get; set; }
    public bool IsBudgetCase { get; set; }
    public DateTime UpdatedAt { get; set; }
}
