namespace FolketingetVotes.Core.Entities;

/// <summary>
/// A private contribution disclosed in a party's accounts. Danish law requires name, address and
/// total amount for donors above an indexed threshold (about 20,000 DKK per year).
/// </summary>
public sealed class PartyDonation
{
    public int Id { get; set; }
    public int PartyAccountId { get; set; }
    public required string DonorName { get; set; }
    public string? DonorAddress { get; set; }
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = "DKK";
    public string? Note { get; set; }
    public int? SourcePage { get; set; }

    /// <summary>The exact text the row was parsed from, kept for auditability.</summary>
    public required string RawText { get; set; }

    public PartyAccount? PartyAccount { get; set; }
}
