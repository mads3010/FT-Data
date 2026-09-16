namespace FolketingetVotes.Core.Entities;

/// <summary>Periode: a parliamentary session (Folketingsår), e.g. "2023-24" with code 20231.</summary>
public sealed class Period : IHasId
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public required string Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime UpdatedAt { get; set; }
}
