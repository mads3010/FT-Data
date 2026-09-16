using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class SessionQueries(FolketingetDbContext db) : ISessionQueries
{
    public async Task<IReadOnlyList<SessionListItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await SessionRows(null).ToListAsync(cancellationToken);
    }

    public async Task<SessionDetail?> GetAsync(int periodId, CancellationToken cancellationToken = default)
    {
        var session = await SessionRows(periodId).FirstOrDefaultAsync(cancellationToken);
        if (session is null)
        {
            return null;
        }

        var parties = await (
            from s in db.PartyStats
            join p in db.Parties on s.PartyShortName equals p.ShortName
            where s.PeriodId == periodId
            orderby s.Members descending, s.PartyShortName
            select new SessionPartyRow(p.ShortName, p.Name, s.Members, s.Ballots, s.PresentBallots, s.WithMajority, s.AgainstMajority)).ToListAsync(cancellationToken);

        var closest = (await VoteProjections.Rows(db)
            .Where(v => db.Meetings.Any(m => m.Id == db.Votes.Where(x => x.Id == v.VoteId).Select(x => x.MeetingId).FirstOrDefault() && m.PeriodId == periodId))
            .Where(v => v.ForCount + v.AgainstCount > 0)
            .OrderBy(v => Math.Abs(v.ForCount - v.AgainstCount)).ThenByDescending(v => v.Date)
            .Take(12).ToListAsync(cancellationToken)).Select(VoteProjections.ToItem).ToList();

        var majorityRows = await (
            from b in db.VotePartyBreakdowns
            join v in db.Votes on b.VoteId equals v.Id
            join m in db.Meetings on v.MeetingId equals m.Id
            where m.PeriodId == periodId && b.PartyShortName != null && b.MajorityBallotType != null
            select new { b.VoteId, Party = b.PartyShortName!, Majority = (int)b.MajorityBallotType! }).ToListAsync(cancellationToken);

        var agreementParties = parties.Where(p => p.ShortName != "UFG" && p.Members >= 1).Select(p => p.ShortName).ToList();
        var agreements = AgreementMath.Compute(majorityRows.Select(r => (r.VoteId, r.Party, r.Majority)), agreementParties);

        return new SessionDetail(session, parties, closest, agreementParties, agreements);
    }

    // The filter is applied before the constructor projection; EF Core cannot translate filters on positional-record members.
    private IQueryable<SessionListItem> SessionRows(int? periodId) =>
        from p in db.Periods
        where (periodId == null || p.Id == periodId) && db.Meetings.Any(m => m.PeriodId == p.Id && db.Votes.Any(v => v.MeetingId == m.Id))
        orderby p.StartDate descending
        select new SessionListItem(
            p.Id, p.Code, p.Title, p.StartDate, p.EndDate,
            db.Votes.Count(v => db.Meetings.Any(m => m.Id == v.MeetingId && m.PeriodId == p.Id)),
            db.Votes.Count(v => v.Passed && db.Meetings.Any(m => m.Id == v.MeetingId && m.PeriodId == p.Id)),
            db.VoteTotals.Count(t => t.AgainstCount == 0 && t.AbstainCount == 0 && t.ForCount > 0
                && db.Votes.Any(v => v.Id == t.VoteId && db.Meetings.Any(m => m.Id == v.MeetingId && m.PeriodId == p.Id))));
}
