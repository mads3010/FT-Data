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

    /// <summary>Where the span comes from; see <see cref="PartyMembershipSource"/>.</summary>
    public int Source { get; set; }
}

/// <summary>Values of <see cref="PartyMembership.Source"/>, in priority order.</summary>
public static class PartyMembershipSource
{
    /// <summary>A group→member relation from the API (preferred).</summary>
    public const int ApiRelation = 1;

    /// <summary>A dated term stated in the member's biography.</summary>
    public const int BiographyTerm = 2;

    /// <summary>The party the biography names, undated (start 1900-01-01); used only for vote attribution, never shown as a membership.</summary>
    public const int BiographyParty = 3;
}
