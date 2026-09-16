using FolketingetVotes.Core.Entities;
using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class PoliticianQueries(FolketingetDbContext db) : IPoliticianQueries
{
    public async Task<PagedResult<PoliticianListItem>> SearchAsync(PoliticianFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var stats = db.PoliticianStats
            .GroupBy(s => s.ActorId)
            .Select(g => new { ActorId = g.Key, Total = g.Sum(s => s.Total), Absent = g.Sum(s => s.AbsentCount) });

        var query =
            from a in db.Actors
            join st in stats on a.Id equals st.ActorId
            where a.TypeId == ActorType.Person
            select new
            {
                a.Id,
                a.Name,
                a.PictureUrl,
                st.Total,
                st.Absent,
                CurrentParty = db.PartyMemberships.Where(pm => pm.PersonId == a.Id).OrderBy(pm => pm.Source == PartyMembershipSource.BiographyParty).ThenByDescending(pm => pm.StartDate).Select(pm => pm.PartyShortName).FirstOrDefault(),
                IsCurrent = db.PartyMemberships.Any(pm => pm.PersonId == a.Id && pm.Source != PartyMembershipSource.BiographyParty && (pm.EndDate == null || pm.EndDate >= today)),
            };

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var pattern = VoteProjections.Pattern(filter.Query);
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filter.PartyShortName))
        {
            var party = filter.PartyShortName;
            query = filter.CurrentOnly
                ? query.Where(x => x.CurrentParty == party && x.IsCurrent)
                : query.Where(x => db.PartyMemberships.Any(pm => pm.PersonId == x.Id && pm.PartyShortName == party && pm.Source != PartyMembershipSource.BiographyParty));
        }
        else if (filter.CurrentOnly)
        {
            query = query.Where(x => x.IsCurrent);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = rows.Select(x => new PoliticianListItem(
            x.Id, x.Name, x.CurrentParty, x.PictureUrl, x.IsCurrent, x.Total, x.Total == 0 ? null : (x.Total - x.Absent) / (double)x.Total)).ToList();
        return new PagedResult<PoliticianListItem>(items, page, pageSize, total);
    }

    public async Task<PoliticianProfile?> GetProfileAsync(int actorId, CancellationToken cancellationToken = default)
    {
        var actor = await db.Actors.AsNoTracking().FirstOrDefaultAsync(a => a.Id == actorId && a.TypeId == ActorType.Person, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var memberships = await (
            from pm in db.PartyMemberships
            join p0 in db.Parties on pm.PartyShortName equals p0.ShortName into pp
            from p in pp.DefaultIfEmpty()
            where pm.PersonId == actorId && pm.Source != PartyMembershipSource.BiographyParty
            orderby pm.StartDate
            select new PartyMembershipRow(pm.PartyShortName, p != null ? p.Name : pm.PartyShortName, pm.StartDate, pm.EndDate)).ToListAsync(cancellationToken);

        var merged = MergeConsecutive(memberships);
        var current = merged.LastOrDefault(m => m.EndDate is null || m.EndDate >= today);
        var biographyParty = merged.Count == 0
            ? await db.PartyMemberships.Where(pm => pm.PersonId == actorId && pm.Source == PartyMembershipSource.BiographyParty).Select(pm => pm.PartyShortName).FirstOrDefaultAsync(cancellationToken)
            : null;

        var perPeriod = await (
            from s in db.PoliticianStats
            join p in db.Periods on s.PeriodId equals p.Id
            where s.ActorId == actorId
            orderby p.StartDate descending
            select new PoliticianPeriodStatsRow(p.Id, p.Title, p.StartDate,
                new PoliticianStats(s.Total, s.ForCount, s.AgainstCount, s.AbstainCount, s.AbsentCount, s.WithPartyCount, s.AgainstPartyCount))).ToListAsync(cancellationToken);

        var overall = perPeriod.Aggregate(PoliticianStats.Empty, (acc, r) => new PoliticianStats(
            acc.Total + r.Stats.Total,
            acc.ForCount + r.Stats.ForCount,
            acc.AgainstCount + r.Stats.AgainstCount,
            acc.AbstainCount + r.Stats.AbstainCount,
            acc.AbsentCount + r.Stats.AbsentCount,
            acc.WithPartyCount + r.Stats.WithPartyCount,
            acc.AgainstPartyCount + r.Stats.AgainstPartyCount));

        return new PoliticianProfile(actor.Id, actor.Name, actor.PictureUrl, current?.PartyShortName ?? merged.LastOrDefault()?.PartyShortName ?? biographyParty, current is not null, merged, overall, perPeriod);
    }

    public async Task<PagedResult<PoliticianBallotRow>> GetBallotsAsync(int actorId, BallotFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var query =
            from b in db.BallotParties
            join v in db.Votes on b.VoteId equals v.Id
            join s0 in db.CaseSteps on v.CaseStepId equals s0.Id into ss
            from s in ss.DefaultIfEmpty()
            join c0 in db.Cases on s.CaseId equals c0.Id into cc
            from c in cc.DefaultIfEmpty()
            join p0 in db.VotePartyBreakdowns on new { b.VoteId, Party = b.PartyShortName } equals new { p0.VoteId, Party = p0.PartyShortName } into pp
            from p in pp.DefaultIfEmpty()
            where b.ActorId == actorId
            select new { b, v, s, c, Majority = p!.MajorityBallotType };

        if (filter.PeriodId is { } periodId)
        {
            query = query.Where(x => x.b.PeriodId == periodId);
        }

        if (filter.Ballot is { } ballot)
        {
            query = query.Where(x => x.b.BallotType == ballot);
        }

        if (filter.VoteType is { } voteType)
        {
            query = query.Where(x => x.v.TypeId == voteType);
        }

        if (filter.DissentOnly)
        {
            query = query.Where(x => x.Majority != null && x.b.BallotType != BallotType.Absent && x.b.BallotType != x.Majority);
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var pattern = VoteProjections.Pattern(filter.Query);
            query = query.Where(x =>
                (x.c != null && (EF.Functions.ILike(x.c.Title, pattern) || EF.Functions.ILike(x.c.ShortTitle!, pattern) || EF.Functions.ILike(x.c.Number!, pattern)))
                || (x.s != null && EF.Functions.ILike(x.s.Title, pattern)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.b.VoteDate).ThenByDescending(x => x.v.Number).ThenByDescending(x => x.v.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PoliticianBallotRow(
                x.v.Id,
                x.b.VoteDate,
                x.c != null ? (x.c.ShortTitle ?? x.c.Title) : (x.s != null ? x.s.Title : "Afstemning nr. " + x.v.Number),
                x.c != null ? x.c.Number : null,
                x.c != null ? x.c.Id : (int?)null,
                x.v.TypeId,
                x.v.Passed,
                x.b.BallotType,
                x.b.PartyShortName,
                x.Majority))
            .ToListAsync(cancellationToken);

        return new PagedResult<PoliticianBallotRow>(items, page, pageSize, total);
    }

    /// <summary>Collapses back-to-back memberships of the same party (one row per session in the source) into one span.</summary>
    internal static List<PartyMembershipRow> MergeConsecutive(IReadOnlyList<PartyMembershipRow> ordered)
    {
        var result = new List<PartyMembershipRow>();
        foreach (var row in ordered)
        {
            if (result.Count > 0)
            {
                var last = result[^1];
                var contiguous = last.PartyShortName == row.PartyShortName
                    && (last.EndDate is null || row.StartDate <= last.EndDate.Value.AddDays(1));
                if (contiguous)
                {
                    var end = last.EndDate is null || row.EndDate is null ? null : (row.EndDate > last.EndDate ? row.EndDate : last.EndDate);
                    result[^1] = last with { EndDate = end };
                    continue;
                }
            }

            result.Add(row);
        }

        return result;
    }
}
