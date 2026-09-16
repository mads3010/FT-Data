namespace FolketingetVotes.Core.Entities;

/// <summary>
/// A party as seen across sessions, keyed by the group short name (S, V, DF, ...).
/// Derived from the per-session parliamentary-group actors; rebuilt by the stats refresh.
/// </summary>
public sealed class Party
{
    public required string ShortName { get; set; }
    public required string Name { get; set; }
    public int? LatestGroupActorId { get; set; }
    public DateOnly? FirstSeen { get; set; }
    public DateOnly? LastSeen { get; set; }
}
