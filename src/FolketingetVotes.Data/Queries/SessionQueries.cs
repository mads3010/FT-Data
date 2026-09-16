using FolketingetVotes.Core.Enums;
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

        // Bills and resolutions that reached a final vote: outcome by category, and days from introduction to that vote.
        var finalVotes = await (
            from c in db.Cases
            join cat0 in db.Lookups.Where(l => l.Kind == LookupKind.CaseCategory) on c.CategoryId equals cat0.Id into cats
            from cat in cats.DefaultIfEmpty()
            join s in db.CaseSteps on c.Id equals s.CaseId
            join v in db.Votes on s.Id equals v.CaseStepId
            join m in db.Meetings on v.MeetingId equals m.Id
            where v.TypeId == VoteType.FinalPassage && c.PeriodId == periodId && (c.TypeId == CaseType.Bill || c.TypeId == CaseType.Resolution)
            select new { c.Id, c.TypeId, Category = cat != null ? cat.Name : "Ukendt", v.Passed, FinalDate = m.Date,
                Introduced = db.CaseSteps.Where(x => x.CaseId == c.Id && x.Date != null).Min(x => x.Date) }).ToListAsync(cancellationToken);
        var perCase = finalVotes.GroupBy(f => f.Id).Select(g => g.OrderByDescending(f => f.FinalDate).First()).ToList();
        var legislation = perCase.GroupBy(f => (f.TypeId, Category: NormaliseCategory(f.Category)))
            .Select(g => new LegislationRow(g.Key.TypeId, g.Key.Category, g.Count(), g.Count(f => f.Passed)))
            .OrderBy(r => r.Type).ThenByDescending(r => r.VotedOn).ToList();
        var medianDays = Median.Of(perCase.Where(f => f.Introduced is not null).Select(f => (f.FinalDate.Date - f.Introduced!.Value.Date).TotalDays));

        var dissentCount = await DissentQuery(periodId).CountAsync(cancellationToken);

        return new SessionDetail(session, parties, closest, agreementParties, agreements, legislation, medianDays, dissentCount);
    }

    public async Task<PagedResult<DissentRow>> GetDissentsAsync(int periodId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = DissentQuery(periodId);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.VoteDate).ThenByDescending(x => x.VoteId).ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var voteIds = rows.Select(r => r.VoteId).Distinct().ToArray();
        var votes = (await VoteProjections.Rows(db).Where(v => voteIds.Contains(v.VoteId)).ToListAsync(cancellationToken)).ToDictionary(v => v.VoteId, VoteProjections.ToItem);
        var items = rows.Select(r => new DissentRow(r.ActorId, r.Name, r.PartyShortName, votes[r.VoteId], r.BallotType, r.Majority)).ToList();
        return new PagedResult<DissentRow>(items, page, pageSize, total);
    }

    private IQueryable<DissentSource> DissentQuery(int periodId) =>
        from b in db.BallotParties
        join p in db.VotePartyBreakdowns on new { b.VoteId, Party = b.PartyShortName } equals new { p.VoteId, Party = p.PartyShortName }
        join a in db.Actors on b.ActorId equals a.Id
        where b.PeriodId == periodId && b.PartyShortName != null && p.MajorityBallotType != null
              && b.BallotType != BallotType.Absent && b.BallotType != p.MajorityBallotType
        select new DissentSource { ActorId = b.ActorId, Name = a.Name, PartyShortName = b.PartyShortName, VoteId = b.VoteId, VoteDate = b.VoteDate, BallotType = b.BallotType, Majority = p.MajorityBallotType!.Value };

    private sealed class DissentSource
    {
        public int ActorId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? PartyShortName { get; init; }
        public int VoteId { get; init; }
        public DateTime VoteDate { get; init; }
        public BallotType BallotType { get; init; }
        public BallotType Majority { get; init; }
    }

    /// <summary>The API's categories split private bills by reading ("B1 - Privat forslag - 1. (eneste) beh."); fold them.</summary>
    private static string NormaliseCategory(string category) =>
        category.Contains("Privat", StringComparison.OrdinalIgnoreCase) ? "Privat forslag"
        : category.Contains("Regering", StringComparison.OrdinalIgnoreCase) ? "Regeringsforslag"
        : category;

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
