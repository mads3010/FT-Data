namespace FolketingetVotes.Core.Entities;

/// <summary>EmneordSag: a keyword assigned to a case.</summary>
public sealed class CaseKeyword : IHasId
{
    public int Id { get; set; }
    public int CaseId { get; set; }
    public int KeywordId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
