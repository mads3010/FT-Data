using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Core.Entities;

/// <summary>Afstemning: one roll-call vote in the chamber.</summary>
public sealed class Vote : IHasId
{
    public int Id { get; set; }
    public int Number { get; set; }

    /// <summary>Official conclusion text listing the outcome and party positions.</summary>
    public string? Conclusion { get; set; }

    public bool Passed { get; set; }
    public string? Comment { get; set; }
    public VoteType TypeId { get; set; }
    public int MeetingId { get; set; }
    public int? CaseStepId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
