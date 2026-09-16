using FolketingetVotes.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Persistence.Views;

/// <summary>Row of <c>mv_ballots</c>: every ballot with the member's party on the day of the vote.</summary>
public sealed class BallotPartyView
{
    public int BallotId { get; init; }
    public int VoteId { get; init; }
    public int ActorId { get; init; }
    public BallotType BallotType { get; init; }
    public DateTime VoteDate { get; init; }
    public int PeriodId { get; init; }
    public string? PartyShortName { get; init; }
}

/// <summary>Row of <c>mv_vote_totals</c>.</summary>
public sealed class VoteTotalsView
{
    public int VoteId { get; init; }
    public int ForCount { get; init; }
    public int AgainstCount { get; init; }
    public int AbstainCount { get; init; }
    public int AbsentCount { get; init; }
}

/// <summary>Row of <c>mv_vote_party_breakdown</c>.</summary>
[Keyless]
public sealed class VotePartyBreakdownView
{
    public int VoteId { get; init; }
    public string? PartyShortName { get; init; }
    public int ForCount { get; init; }
    public int AgainstCount { get; init; }
    public int AbstainCount { get; init; }
    public int AbsentCount { get; init; }
    public BallotType? MajorityBallotType { get; init; }
}

/// <summary>Row of <c>mv_politician_stats</c>: one politician's counts within one session.</summary>
public sealed class PoliticianStatsView
{
    public int ActorId { get; init; }
    public int PeriodId { get; init; }
    public int Total { get; init; }
    public int ForCount { get; init; }
    public int AgainstCount { get; init; }
    public int AbstainCount { get; init; }
    public int AbsentCount { get; init; }
    public int WithPartyCount { get; init; }
    public int AgainstPartyCount { get; init; }
}

/// <summary>Row of <c>mv_party_stats</c>: one party's aggregate within one session.</summary>
public sealed class PartyStatsView
{
    public string PartyShortName { get; init; } = string.Empty;
    public int PeriodId { get; init; }
    public int Members { get; init; }
    public int Ballots { get; init; }
    public int PresentBallots { get; init; }
    public int WithMajority { get; init; }
    public int AgainstMajority { get; init; }
}

internal sealed class MaterializedViewConfigurations :
    IEntityTypeConfiguration<BallotPartyView>,
    IEntityTypeConfiguration<VoteTotalsView>,
    IEntityTypeConfiguration<VotePartyBreakdownView>,
    IEntityTypeConfiguration<PoliticianStatsView>,
    IEntityTypeConfiguration<PartyStatsView>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<BallotPartyView> b)
    {
        b.ToView("mv_ballots");
        b.HasKey(x => x.BallotId);
        b.Property(x => x.BallotType).HasConversion<int>();
    }

    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<VoteTotalsView> b)
    {
        b.ToView("mv_vote_totals");
        b.HasKey(x => x.VoteId);
    }

    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<VotePartyBreakdownView> b)
    {
        b.ToView("mv_vote_party_breakdown");
        b.Property(x => x.MajorityBallotType).HasConversion<int?>();
    }

    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PoliticianStatsView> b)
    {
        b.ToView("mv_politician_stats");
        b.HasKey(x => new { x.ActorId, x.PeriodId });
    }

    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<PartyStatsView> b)
    {
        b.ToView("mv_party_stats");
        b.HasKey(x => new { x.PartyShortName, x.PeriodId });
    }
}
