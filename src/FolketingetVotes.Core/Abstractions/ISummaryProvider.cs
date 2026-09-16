namespace FolketingetVotes.Core.Abstractions;

/// <summary>
/// Supplies a machine-generated summary for a case. v1 ships <see cref="NullSummaryProvider"/>;
/// an AI-backed implementation can be registered in its place (see docs/roadmap.md).
/// </summary>
public interface ISummaryProvider
{
    Task<BillSummaryContent?> GetSummaryAsync(int caseId, CancellationToken cancellationToken = default);
}

/// <summary>The structured content of a generated summary.</summary>
public sealed record BillSummaryContent(
    string WhatItIsAbout,
    string WhatItDoes,
    string WhyItIsProposed,
    string ProposedBy,
    string Provider,
    string Model,
    DateTime GeneratedAt,
    string? SourceUrl);

/// <summary>Default provider: no summaries are available.</summary>
public sealed class NullSummaryProvider : ISummaryProvider
{
    public Task<BillSummaryContent?> GetSummaryAsync(int caseId, CancellationToken cancellationToken = default)
        => Task.FromResult<BillSummaryContent?>(null);
}
