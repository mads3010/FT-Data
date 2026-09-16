using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class SiteQueries(FolketingetDbContext db) : ISiteQueries
{
    public async Task<SiteOverview> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var lastSync = await db.SyncStates.MaxAsync(s => s.LastRunCompletedAt, cancellationToken);
        var voteCount = await db.Votes.CountAsync(cancellationToken);
        var ballotCount = await db.Ballots.LongCountAsync(cancellationToken);
        var politicianCount = await db.PoliticianStats.Select(s => s.ActorId).Distinct().CountAsync(cancellationToken);

        var voteDates =
            from v in db.Votes
            join m in db.Meetings on v.MeetingId equals m.Id
            select m.Date;
        var earliest = await voteDates.MinAsync(d => (DateTime?)d, cancellationToken);
        var latest = await voteDates.MaxAsync(d => (DateTime?)d, cancellationToken);

        var latestVotes = (await VoteProjections.Rows(db)
            .OrderByDescending(v => v.Date).ThenByDescending(v => v.VoteId)
            .Take(10)
            .ToListAsync(cancellationToken)).Select(VoteProjections.ToItem).ToList();

        return new SiteOverview(lastSync, voteCount, ballotCount, politicianCount, earliest, latest, latestVotes);
    }

    public async Task<IReadOnlyList<SitemapEntry>> GetSitemapAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<SitemapEntry> { new("/", null), new("/afstemninger", null), new("/politikere", null), new("/partier", null), new("/sager", null), new("/emner", null), new("/folketingsaar", null), new("/bidrag", null), new("/om", null) };
        entries.AddRange(await db.Votes.Select(v => new SitemapEntry("/afstemninger/" + v.Id, v.UpdatedAt)).ToListAsync(cancellationToken));
        entries.AddRange(await db.PoliticianStats.Select(s => s.ActorId).Distinct().Select(id => new SitemapEntry("/politikere/" + id, null)).ToListAsync(cancellationToken));
        entries.AddRange(await db.Parties.Select(p => new SitemapEntry("/partier/" + p.ShortName, null)).ToListAsync(cancellationToken));
        entries.AddRange(await db.Cases.Where(c => db.CaseSteps.Any(s => s.CaseId == c.Id && db.Votes.Any(v => v.CaseStepId == s.Id))).Select(c => new SitemapEntry("/sager/" + c.Id, c.UpdatedAt)).ToListAsync(cancellationToken));
        entries.AddRange(await db.TopicStats.Where(t => t.VoteCount > 0).Select(t => new SitemapEntry("/emner/" + t.KeywordId, null)).ToListAsync(cancellationToken));
        entries.AddRange(await (from p in db.Periods where db.Meetings.Any(m => m.PeriodId == p.Id && db.Votes.Any(v => v.MeetingId == m.Id)) select new SitemapEntry("/folketingsaar/" + p.Id, null)).ToListAsync(cancellationToken));
        return entries;
    }

    public async Task<IReadOnlyList<PeriodOption>> GetPeriodsWithVotesAsync(CancellationToken cancellationToken = default)
    {
        return await (
            from p in db.Periods
            where db.Meetings.Any(m => m.PeriodId == p.Id && db.Votes.Any(v => v.MeetingId == m.Id))
            orderby p.StartDate descending
            select new PeriodOption(p.Id, p.Code, p.Title, p.StartDate)).ToListAsync(cancellationToken);
    }
}

internal static class LookupKindNames
{
    public static string BallotLabel(BallotType type) => type switch
    {
        BallotType.For => "For",
        BallotType.Against => "Imod",
        BallotType.Abstain => "Hverken for eller imod",
        BallotType.Absent => "Fraværende",
        _ => type.ToString(),
    };
}
