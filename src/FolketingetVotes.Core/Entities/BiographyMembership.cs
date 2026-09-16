namespace FolketingetVotes.Core.Entities;

/// <summary>
/// A parliamentary term for a party as stated in the member's own biography ("Folketingsmedlem for X i Y, dato – dato").
/// Secondary source for party attribution where the API has no group membership relation.
/// </summary>
public sealed class BiographyMembership
{
    public int Id { get; set; }
    public int PersonId { get; set; }
    public required string PartyName { get; set; }
    public string? PartyShortName { get; set; }
    public string? Constituency { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
