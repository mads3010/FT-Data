namespace FolketingetVotes.Core.Entities;

/// <summary>AktørAktør: a dated relation between two actors, e.g. group → member.</summary>
public sealed class ActorRelation : IHasId
{
    public int Id { get; set; }
    public int FromActorId { get; set; }
    public int ToActorId { get; set; }
    public int RoleId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime UpdatedAt { get; set; }
}
