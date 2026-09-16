namespace FolketingetVotes.Core.Entities;

/// <summary>Møde: a sitting of the chamber (or a committee meeting).</summary>
public sealed class Meeting : IHasId
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Room { get; set; }
    public string? Number { get; set; }
    public DateTime Date { get; set; }
    public int StatusId { get; set; }
    public int TypeId { get; set; }
    public int PeriodId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
