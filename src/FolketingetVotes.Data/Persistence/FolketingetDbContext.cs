using FolketingetVotes.Core.Entities;
using FolketingetVotes.Data.Persistence.Views;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Persistence;

/// <summary>
/// The application's PostgreSQL schema. Tables mirror oda.ft.dk entity sets (snake_case naming);
/// <c>mv_*</c> materialized views are created by <see cref="Stats.StatsRefresher"/>, not by migrations.
/// </summary>
public sealed class FolketingetDbContext(DbContextOptions<FolketingetDbContext> options) : DbContext(options)
{
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<ActorRelation> ActorRelations => Set<ActorRelation>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<ParliamentaryCase> Cases => Set<ParliamentaryCase>();
    public DbSet<CaseStep> CaseSteps => Set<CaseStep>();
    public DbSet<CaseActor> CaseActors => Set<CaseActor>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<Ballot> Ballots => Set<Ballot>();
    public DbSet<Lookup> Lookups => Set<Lookup>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<PartyMembership> PartyMemberships => Set<PartyMembership>();
    public DbSet<BiographyMembership> BiographyMemberships => Set<BiographyMembership>();
    public DbSet<Keyword> Keywords => Set<Keyword>();
    public DbSet<CaseKeyword> CaseKeywords => Set<CaseKeyword>();
    public DbSet<RolePeriod> RolePeriods => Set<RolePeriod>();
    public DbSet<BillSummary> BillSummaries => Set<BillSummary>();
    public DbSet<PartyAccount> PartyAccounts => Set<PartyAccount>();
    public DbSet<PartyDonation> PartyDonations => Set<PartyDonation>();

    // Materialized views (read-only)
    public DbSet<BallotPartyView> BallotParties => Set<BallotPartyView>();
    public DbSet<VoteTotalsView> VoteTotals => Set<VoteTotalsView>();
    public DbSet<VotePartyBreakdownView> VotePartyBreakdowns => Set<VotePartyBreakdownView>();
    public DbSet<PoliticianStatsView> PoliticianStats => Set<PoliticianStatsView>();
    public DbSet<PartyStatsView> PartyStats => Set<PartyStatsView>();
    public DbSet<CurrentMemberView> CurrentMembers => Set<CurrentMemberView>();
    public DbSet<TopicStatsView> TopicStats => Set<TopicStatsView>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // oda.ft.dk timestamps carry no offset; store them as-is (Danish local time).
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FolketingetDbContext).Assembly);
    }
}
