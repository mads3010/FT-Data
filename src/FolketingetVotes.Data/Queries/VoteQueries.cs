using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class VoteQueries(FolketingetDbContext db) : IVoteQueries
{
    public async Task<PagedResult<VoteListItem>> SearchAsync(VoteFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var query =
            from v in db.Votes
            join m in db.Meetings on v.MeetingId equals m.Id
            join s0 in db.CaseSteps on v.CaseStepId equals s0.Id into ss
            from s in ss.DefaultIfEmpty()
            join c0 in db.Cases on s.CaseId equals c0.Id into cc
            from c in cc.DefaultIfEmpty()
            select new { v, m, s, c };

        if (filter.PeriodId is { } periodId)
        {
            query = query.Where(x => x.m.PeriodId == periodId);
        }

        if (filter.Type is { } type)
        {
            query = query.Where(x => x.v.TypeId == type);
        }

        if (filter.Passed is { } passed)
        {
            query = query.Where(x => x.v.Passed == passed);
        }

        if (filter.CaseType is { } caseType)
        {
            query = query.Where(x => x.c != null && x.c.TypeId == caseType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var pattern = VoteProjections.Pattern(filter.Query);
            query = query.Where(x =>
                (x.c != null && (EF.Functions.ILike(x.c.Title, pattern) || EF.Functions.ILike(x.c.ShortTitle!, pattern) || EF.Functions.ILike(x.c.Number!, pattern)))
                || (x.s != null && EF.Functions.ILike(x.s.Title, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);
        var ids = await query
            .OrderByDescending(x => x.m.Date).ThenByDescending(x => x.v.Number).ThenByDescending(x => x.v.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => x.v.Id)
            .ToListAsync(cancellationToken);

        var rows = await VoteProjections.Rows(db).Where(x => ids.Contains(x.VoteId)).ToListAsync(cancellationToken);
        var ordered = ids.Select(id => VoteProjections.ToItem(rows.First(i => i.VoteId == id))).ToList();
        return new PagedResult<VoteListItem>(ordered, page, pageSize, total);
    }

    public async Task<VoteDetail?> GetAsync(int voteId, CancellationToken cancellationToken = default)
    {
        var row = await VoteProjections.Rows(db).FirstOrDefaultAsync(x => x.VoteId == voteId, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var item = VoteProjections.ToItem(row);

        var header = await (
            from v in db.Votes
            join m in db.Meetings on v.MeetingId equals m.Id
            join p in db.Periods on m.PeriodId equals p.Id
            where v.Id == voteId
            select new { v.Conclusion, v.Comment, MeetingId = m.Id, MeetingTitle = m.Title, PeriodId = p.Id, PeriodTitle = p.Title }).SingleAsync(cancellationToken);

        CaseSummary? caseSummary = null;
        if (item.CaseId is { } caseId)
        {
            caseSummary = await CaseQueries.SummaryAsync(db, caseId, cancellationToken);
        }

        var partyNames = await db.Parties.ToDictionaryAsync(p => p.ShortName, p => p.Name, cancellationToken);
        var breakdown = await db.VotePartyBreakdowns.Where(b => b.VoteId == voteId).ToListAsync(cancellationToken);
        var parties = breakdown
            .Select(b => new PartyVoteBreakdown(
                b.PartyShortName,
                b.PartyShortName is null ? "Ukendt gruppe" : partyNames.GetValueOrDefault(b.PartyShortName, b.PartyShortName),
                b.ForCount, b.AgainstCount, b.AbstainCount, b.AbsentCount))
            .OrderByDescending(p => p.Total).ThenBy(p => p.PartyShortName)
            .ToList();
        var majorities = breakdown.Where(b => b.PartyShortName is not null).ToDictionary(b => b.PartyShortName!, b => b.MajorityBallotType);

        var ballots = await (
            from b in db.BallotParties
            join a in db.Actors on b.ActorId equals a.Id
            where b.VoteId == voteId
            orderby a.Name
            select new { b.ActorId, a.Name, b.PartyShortName, b.BallotType }).ToListAsync(cancellationToken);

        var rows = ballots.Select(b =>
        {
            var majority = b.PartyShortName is null ? null : majorities.GetValueOrDefault(b.PartyShortName);
            var dissents = majority is not null && b.BallotType != BallotType.Absent && b.BallotType != majority;
            return new BallotRow(b.ActorId, b.Name, b.PartyShortName, b.BallotType, dissents);
        }).ToList();

        return new VoteDetail(item, header.Conclusion, header.Comment, header.MeetingId, header.MeetingTitle, header.PeriodId, header.PeriodTitle, caseSummary, parties, rows);
    }
}
