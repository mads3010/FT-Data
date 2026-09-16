using FolketingetVotes.Core.Entities;
using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using FolketingetVotes.Data.Queries;
using FolketingetVotes.Data.Stats;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FolketingetVotes.Data.Tests;

/// <summary>End-to-end over a real database: seed a tiny parliament, refresh stats, query it back.</summary>
[Collection(PostgresCollection.Name)]
public class StatsPipelineTests(PostgresFixture postgres)
{
    [SkippableFact]
    public async Task Seeded_vote_produces_party_breakdown_and_politician_stats()
    {
        Skip.IfNot(postgres.IsAvailable, "Docker is not available; skipping database test.");

        await using (var db = postgres.CreateContext())
        {
            await Seed(db);
        }

        var factory = new SingleContextFactory(postgres);
        await new StatsRefresher(factory, NullLogger<StatsRefresher>.Instance).RefreshAsync();

        await using var query = postgres.CreateContext();
        var votes = new VoteQueries(query);
        var detail = await votes.GetAsync(1000);

        Assert.NotNull(detail);
        Assert.Equal(2, detail.Vote.ForCount);
        Assert.Equal(1, detail.Vote.AgainstCount);
        Assert.Equal(1, detail.Vote.AbsentCount);

        var red = detail.Parties.Single(p => p.PartyShortName == "RØD");
        Assert.Equal((2, 1, 0), (red.ForCount, red.AgainstCount, red.AbsentCount));
        Assert.Equal(BallotType.For, red.Majority);

        var rebel = detail.Ballots.Single(b => b.Name == "Rebel Rødsen");
        Assert.True(rebel.DissentsFromParty);

        var politicians = new PoliticianQueries(query);
        var profile = await politicians.GetProfileAsync(3);
        Assert.NotNull(profile);
        Assert.Equal(1, profile.Overall.AgainstPartyCount);
        Assert.Equal("RØD", profile.CurrentPartyShortName);

        var blueProfile = await politicians.GetProfileAsync(4);
        Assert.NotNull(blueProfile);
        Assert.Equal(0.0, blueProfile.Overall.AttendanceRate);

        // Current members = everyone registered in the latest sitting day's votes, with their group that day.
        var parties = new PartyQueries(query);
        var redParty = await parties.GetAsync("RØD");
        Assert.NotNull(redParty);
        Assert.Equal(3, redParty.CurrentMembers.Count);
        var sessions = new SessionQueries(query);
        var session = await sessions.GetAsync(1);
        Assert.NotNull(session);
        Assert.Equal(1, session.Session.VoteCount);
        Assert.Contains(session.Agreements, a => a.SharedVotes == 0 || a.AgreedVotes <= a.SharedVotes);

        var search = await votes.SearchAsync(new VoteFilter(Query: "Prøve"), 1, 10);
        Assert.Equal(1, search.TotalCount);
    }

    private static async Task Seed(FolketingetDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE periods, actors, actor_relations, meetings, cases, case_steps, case_actors, votes, ballots, lookups, sync_states, parties, party_memberships, biography_memberships, role_periods, keywords, case_keywords");
        var now = new DateTime(2024, 1, 1);
        db.Periods.Add(new Period { Id = 1, Code = "20231", Title = "2023-24", Type = "samling", StartDate = new DateTime(2023, 10, 3), EndDate = new DateTime(2024, 10, 1), UpdatedAt = now });
        db.Actors.AddRange(
            new Actor { Id = 10, TypeId = ActorType.ParliamentaryGroup, GroupShortName = "RØD", Name = "Røde Parti", PeriodId = 1, StartDate = new DateTime(2023, 10, 3), EndDate = new DateTime(2024, 10, 1), UpdatedAt = now },
            new Actor { Id = 11, TypeId = ActorType.ParliamentaryGroup, GroupShortName = "BLÅ", Name = "Blå Parti", PeriodId = 1, StartDate = new DateTime(2023, 10, 3), EndDate = new DateTime(2024, 10, 1), UpdatedAt = now },
            new Actor { Id = 1, TypeId = ActorType.Person, Name = "Anna Rødsen", UpdatedAt = now },
            new Actor { Id = 2, TypeId = ActorType.Person, Name = "Bo Rødsen", UpdatedAt = now },
            new Actor { Id = 3, TypeId = ActorType.Person, Name = "Rebel Rødsen", UpdatedAt = now },
            new Actor { Id = 4, TypeId = ActorType.Person, Name = "Dorte Blåsen", UpdatedAt = now });
        db.ActorRelations.AddRange(
            new ActorRelation { Id = 1, FromActorId = 10, ToActorId = 1, RoleId = 15, UpdatedAt = now },
            new ActorRelation { Id = 2, FromActorId = 10, ToActorId = 2, RoleId = 15, UpdatedAt = now },
            new ActorRelation { Id = 3, FromActorId = 10, ToActorId = 3, RoleId = 15, StartDate = new DateTime(2023, 11, 1), UpdatedAt = now },
            new ActorRelation { Id = 4, FromActorId = 11, ToActorId = 4, RoleId = 15, UpdatedAt = now });
        db.Meetings.Add(new Meeting { Id = 100, Title = "Møde i Salen", Number = "1", Date = new DateTime(2024, 3, 5, 10, 0, 0), StatusId = 2, TypeId = 1, PeriodId = 1, UpdatedAt = now });
        db.Cases.Add(new ParliamentaryCase { Id = 500, TypeId = CaseType.Bill, StatusId = 10, Title = "Forslag til lov om prøvesager", ShortTitle = "Prøvesag", Number = "L 1", PeriodId = 1, UpdatedAt = now });
        db.CaseSteps.Add(new CaseStep { Id = 700, CaseId = 500, Title = "3. behandling", Date = new DateTime(2024, 3, 5), TypeId = 17, StatusId = 41, UpdatedAt = now });
        db.Votes.Add(new Vote { Id = 1000, Number = 1, Passed = true, TypeId = VoteType.FinalPassage, MeetingId = 100, CaseStepId = 700, UpdatedAt = now });
        db.Ballots.AddRange(
            new Ballot { Id = 1, VoteId = 1000, ActorId = 1, TypeId = BallotType.For, UpdatedAt = now },
            new Ballot { Id = 2, VoteId = 1000, ActorId = 2, TypeId = BallotType.For, UpdatedAt = now },
            new Ballot { Id = 3, VoteId = 1000, ActorId = 3, TypeId = BallotType.Against, UpdatedAt = now },
            new Ballot { Id = 4, VoteId = 1000, ActorId = 4, TypeId = BallotType.Absent, UpdatedAt = now });
        await db.SaveChangesAsync();
    }

    private sealed class SingleContextFactory(PostgresFixture fixture) : IDbContextFactory<FolketingetDbContext>
    {
        public FolketingetDbContext CreateDbContext() => fixture.CreateContext();
    }
}
