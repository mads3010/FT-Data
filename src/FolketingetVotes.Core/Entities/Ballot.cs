using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Core.Entities;

/// <summary>Stemme: one member's ballot in one vote.</summary>
public sealed class Ballot : IHasId
{
    public int Id { get; set; }
    public int VoteId { get; set; }
    public int ActorId { get; set; }
    public BallotType TypeId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
