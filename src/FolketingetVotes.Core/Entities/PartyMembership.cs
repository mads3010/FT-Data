namespace FolketingetVotes.Core.Entities;

/// <summary>
/// A person's membership of a parliamentary group during a date range.
/// Derived from <see cref="ActorRelation"/> (role "medlem") joined to per-session group actors, with
/// <see cref="BiographyMembership"/> spans as a fallback where the API has no relation.
/// </summary>
public sealed class PartyMembership
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public int GroupActorId { get; set; }
    public required string PartyShortName { get; set; }
    public int? PeriodId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>1 = group relation from the API (preferred), 2 = a term in the member's biography, 3 = the party the biography names (undated last resort).</summary>
    public int Source { get; set; }
}
