namespace FolketingetVotes.Core.Entities;

/// <summary>SagAktør: an actor's role on a case, e.g. proposer or responsible minister.</summary>
public sealed class CaseActor : IHasId
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int ActorId { get; set; }
    public int RoleId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
