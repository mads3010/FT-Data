using FolketingetVotes.Core.Enums;

namespace FolketingetVotes.Core.Entities;

/// <summary>One row of one of the API's code tables (Sagstype, Sagsstatus, ...).</summary>
public sealed class Lookup
{
    public LookupKind Kind { get; set; }
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime UpdatedAt { get; set; }
}
