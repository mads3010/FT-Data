using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Core.Entities;

/// <summary>
/// Aktør: a person, parliamentary group, committee, ministry etc.
/// Parliamentary groups exist once per session, so party membership over time is derived
/// from <see cref="ActorRelation"/> rows linking a group (per session) to a person.
/// </summary>
public sealed class Actor : IHasId
{
    public int Id { get; set; }
    public ActorType TypeId { get; set; }
    public string? GroupShortName { get; set; }
    public required string Name { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? BiographyXml { get; set; }
    public int? PeriodId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Portrait URL parsed from the biography XML (pictureMiRes), if any.</summary>
    public string? PictureUrl { get; set; }

    /// <summary>Party short name parsed from the biography XML (partyShortname), if any.</summary>
    public string? BiographyPartyShortName { get; set; }
}
