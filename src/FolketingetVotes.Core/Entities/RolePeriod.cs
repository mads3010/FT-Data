namespace FolketingetVotes.Core.Entities;

/// <summary>Kinds of <see cref="RolePeriod"/>.</summary>
public enum RolePeriodKind
{
    /// <summary>Held a ministerial post (relation to a "ministertitel" actor).</summary>
    Minister = 1,

    /// <summary>Sat as a temporary member / substitute ("Midlertidigt folketingsmedlem" in the biography).</summary>
    TemporaryMember = 2,

    /// <summary>On leave (orlov) from the Folketing, with or without pay.</summary>
    Leave = 3,
}

/// <summary>
/// A dated period in which a person held a role that changes how their voting record should be read.
/// Derived table, rebuilt by the statistics refresh.
/// </summary>
public sealed class RolePeriod
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public RolePeriodKind Kind { get; set; }
    public required string Title { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
