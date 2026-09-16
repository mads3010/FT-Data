namespace FolketingetVotes.Core.Entities;

/// <summary>One party's annual accounts as published by Folketinget (partiregnskab).</summary>
public sealed class PartyAccount
{
    public int Id { get; set; }
    public int Year { get; set; }
    public required string PartyName { get; set; }
    public string? PartyShortName { get; set; }
    public required string SourceFile { get; set; }
    public int? SourcePage { get; set; }
    public DateTime ImportedAt { get; set; }
    public ICollection<PartyDonation> Donations { get; set; } = [];
}
