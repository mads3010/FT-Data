namespace FolketingetVotes.Core.Entities;

/// <summary>Sagstrin: a step in a case's life, e.g. "1. behandling", "3. behandling".</summary>
public sealed class CaseStep : IHasId
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public required string Title { get; set; }
    public DateTime? Date { get; set; }
    public int TypeId { get; set; }
    public int StatusId { get; set; }
    public string? FolketingstidendeUrl { get; set; }
    public DateTime UpdatedAt { get; set; }
}
