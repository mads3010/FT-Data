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
