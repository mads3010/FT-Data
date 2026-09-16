namespace FolketingetVotes.Core.Entities;

/// <summary>Emneord: a subject keyword attached to cases. Type 1 = sagsområde (broad area), 3 = controlled vocabulary, 2 = free.</summary>
public sealed class Keyword : IHasId
{
    public int Id { get; set; }
    public int TypeId { get; set; }
    public required string Name { get; set; }
    public DateTime UpdatedAt { get; set; }
}
