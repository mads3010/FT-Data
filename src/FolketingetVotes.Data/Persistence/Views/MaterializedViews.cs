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

    /// <summary>The member held a ministerial post on the day of the vote.</summary>
    public bool WhileMinister { get; init; }

    /// <summary>The member was on leave (orlov) on the day of the vote.</summary>
    public bool WhileOnLeave { get; init; }
}

/// <summary>Row of <c>mv_questions</c>: a § 20 question with its parties and dates.</summary>
public sealed class QuestionView
{
    public int CaseId { get; init; }
    public int PeriodId { get; init; }
    public string? Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public int? AskerId { get; init; }
    public string? AskerParty { get; init; }
    public int? MinisterPersonId { get; init; }
    public string? MinisterTitle { get; init; }
    public DateTime? AskedDate { get; init; }
    public DateTime? AnsweredDate { get; init; }
    public bool Oral { get; init; }
    public bool Withdrawn { get; init; }
}

/// <summary>Row of <c>mv_current_members</c>: who is a member today, and of which group.</summary>
public sealed class CurrentMemberView
{
    public int PersonId { get; init; }
    public string PartyShortName { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
}

/// <summary>Row of <c>mv_topic_stats</c>.</summary>
public sealed class TopicStatsView
{
    public int KeywordId { get; init; }
    public int CaseCount { get; init; }
    public int VoteCount { get; init; }
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
    public int MinisterTotal { get; init; }
    public int MinisterAbsent { get; init; }
    public int RoleTotal { get; init; }
    public int RoleAbsent { get; init; }
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
    IEntityTypeConfiguration<CurrentMemberView>,
    IEntityTypeConfiguration<TopicStatsView>,
    IEntityTypeConfiguration<QuestionView>,
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

    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CurrentMemberView> b)
    {
        b.ToView("mv_current_members");
        b.HasKey(x => x.PersonId);
    }

    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TopicStatsView> b)
    {
        b.ToView("mv_topic_stats");
        b.HasKey(x => x.KeywordId);
    }

    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<QuestionView> b)
    {
        b.ToView("mv_questions");
        b.HasKey(x => x.CaseId);
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
