using FolketingetVotes.Core.Entities;
using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class PoliticianQueries(FolketingetDbContext db) : IPoliticianQueries
{
    private static readonly int[] ProposerRoles = [(int)CaseActorRole.ProposerRegistered, (int)CaseActorRole.ProposerPrivate, (int)CaseActorRole.Minister];

    public async Task<PagedResult<PoliticianListItem>> SearchAsync(PoliticianFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var query = ListQuery();

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var pattern = VoteProjections.Pattern(filter.Query);
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(filter.PartyShortName))
        {
            var party = filter.PartyShortName;
            query = filter.CurrentOnly
                ? query.Where(x => x.CurrentParty == party)
                : query.Where(x => db.PartyMemberships.Any(pm => pm.PersonId == x.Id && pm.PartyShortName == party && pm.Source != PartyMembershipSource.BiographyParty));
        }
        else if (filter.CurrentOnly)
        {
            query = query.Where(x => x.IsCurrent);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<PoliticianListItem>(rows.Select(ToListItem).ToList(), page, pageSize, total);
    }

    public async Task<PoliticianProfile?> GetProfileAsync(int actorId, CancellationToken cancellationToken = default)
    {
        var actor = await db.Actors.AsNoTracking().FirstOrDefaultAsync(a => a.Id == actorId && a.TypeId == ActorType.Person, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        var current = await db.CurrentMembers.AsNoTracking().FirstOrDefaultAsync(c => c.PersonId == actorId, cancellationToken);
        var memberships = await (
            from pm in db.PartyMemberships
            join p0 in db.Parties on pm.PartyShortName equals p0.ShortName into pp
            from p in pp.DefaultIfEmpty()
            where pm.PersonId == actorId && pm.Source != PartyMembershipSource.BiographyParty
            orderby pm.StartDate
            select new PartyMembershipRow(pm.PartyShortName, p != null ? p.Name : pm.PartyShortName, pm.StartDate, pm.EndDate)).ToListAsync(cancellationToken);
        var merged = MergeConsecutive(memberships);
        var biographyParty = merged.Count == 0
            ? await db.PartyMemberships.Where(pm => pm.PersonId == actorId && pm.Source == PartyMembershipSource.BiographyParty).Select(pm => pm.PartyShortName).FirstOrDefaultAsync(cancellationToken)
            : null;

        var perPeriod = await (
            from s in db.PoliticianStats
            join p in db.Periods on s.PeriodId equals p.Id
            where s.ActorId == actorId
            orderby p.StartDate descending
            select new PoliticianPeriodStatsRow(p.Id, p.Title, p.StartDate,
                new PoliticianStats(s.Total, s.ForCount, s.AgainstCount, s.AbstainCount, s.AbsentCount, s.WithPartyCount, s.AgainstPartyCount, s.MinisterTotal, s.MinisterAbsent))).ToListAsync(cancellationToken);

        var overall = perPeriod.Aggregate(PoliticianStats.Empty, (acc, r) => new PoliticianStats(
            acc.Total + r.Stats.Total,
            acc.ForCount + r.Stats.ForCount,
            acc.AgainstCount + r.Stats.AgainstCount,
            acc.AbstainCount + r.Stats.AbstainCount,
            acc.AbsentCount + r.Stats.AbsentCount,
            acc.WithPartyCount + r.Stats.WithPartyCount,
            acc.AgainstPartyCount + r.Stats.AgainstPartyCount,
            acc.MinisterTotal + r.Stats.MinisterTotal,
            acc.MinisterAbsent + r.Stats.MinisterAbsent));

        var roles = await db.RolePeriods.AsNoTracking()
            .Where(r => r.PersonId == actorId)
            .OrderByDescending(r => r.StartDate)
            .Select(r => new RolePeriodRow(r.Kind, r.Title, r.StartDate, r.EndDate))
            .ToListAsync(cancellationToken);

        var today = DateTime.Today;
        var committees = await (
            from r in db.ActorRelations
            join c in db.Actors on r.FromActorId equals c.Id
            where r.ToActorId == actorId && r.RoleId == (int)ActorRelationRole.Member && c.TypeId == ActorType.Committee
                  && (r.EndDate == null || r.EndDate >= today)
            select c.Name).Distinct().OrderBy(n => n).ToListAsync(cancellationToken);

        var proposals = await (
            from ca in db.CaseActors
            join c in db.Cases on ca.CaseId equals c.Id
            join p in db.Periods on c.PeriodId equals p.Id
            join st0 in db.Lookups.Where(l => l.Kind == LookupKind.CaseStatus) on c.StatusId equals st0.Id into sts
            from st in sts.DefaultIfEmpty()
            join r0 in db.Lookups.Where(l => l.Kind == LookupKind.CaseActorRole) on ca.RoleId equals r0.Id into rr
            from r in rr.DefaultIfEmpty()
            where ca.ActorId == actorId && ProposerRoles.Contains(ca.RoleId) && (c.TypeId == CaseType.Bill || c.TypeId == CaseType.Resolution)
            orderby p.StartDate descending, c.NumberNumeric descending
            select new CaseListItem(c.Id, c.TypeId, c.Number, c.ShortTitle ?? c.Title, st != null ? st.Name : null, p.Id, p.Title,
                db.Votes.Count(v => db.CaseSteps.Any(s => s.Id == v.CaseStepId && s.CaseId == c.Id)), r != null ? r.Name : null))
            .Take(150).ToListAsync(cancellationToken);

        return new PoliticianProfile(
            actor.Id, actor.Name, actor.PictureUrl,
            current?.PartyShortName ?? merged.LastOrDefault()?.PartyShortName ?? biographyParty,
            current is not null, merged, overall, perPeriod,
            actor.Born?.Year,
            roles.Where(r => r.Kind == RolePeriodKind.Minister).ToList(),
            roles.Where(r => r.Kind == RolePeriodKind.TemporaryMember).ToList(),
            committees, proposals);
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

    /// <summary>One politician as a list item (used by the comparison page).</summary>
    internal async Task<PoliticianListItem?> GetListItemAsync(int actorId, CancellationToken cancellationToken)
    {
        var row = await ListQuery().FirstOrDefaultAsync(x => x.Id == actorId, cancellationToken);
        return row is null ? null : ToListItem(row);
    }

    private IQueryable<ListRow> ListQuery()
    {
        var stats = db.PoliticianStats
            .GroupBy(s => s.ActorId)
            .Select(g => new { ActorId = g.Key, Total = g.Sum(s => s.Total), Absent = g.Sum(s => s.AbsentCount) });

        return
            from a in db.Actors
            join st in stats on a.Id equals st.ActorId
            join cm0 in db.CurrentMembers on a.Id equals cm0.PersonId into cms
            from cm in cms.DefaultIfEmpty()
            where a.TypeId == ActorType.Person
            select new ListRow
            {
                Id = a.Id,
                Name = a.Name,
                PictureUrl = a.PictureUrl,
                Born = a.Born,
                Total = st.Total,
                Absent = st.Absent,
                CurrentParty = cm != null ? cm.PartyShortName : null,
                LastParty = db.PartyMemberships.Where(pm => pm.PersonId == a.Id && pm.Source != PartyMembershipSource.BiographyParty).OrderByDescending(pm => pm.StartDate).Select(pm => pm.PartyShortName).FirstOrDefault(),
                IsCurrent = cm != null,
            };
    }

    private static PoliticianListItem ToListItem(ListRow x) => new(
        x.Id, x.Name, x.CurrentParty ?? x.LastParty, x.PictureUrl, x.IsCurrent, x.Total,
        x.Total == 0 ? null : (x.Total - x.Absent) / (double)x.Total, x.Born?.Year);

    private sealed class ListRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? PictureUrl { get; init; }
        public DateOnly? Born { get; init; }
        public int Total { get; init; }
        public int Absent { get; init; }
        public string? CurrentParty { get; init; }
        public string? LastParty { get; init; }
        public bool IsCurrent { get; init; }
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
