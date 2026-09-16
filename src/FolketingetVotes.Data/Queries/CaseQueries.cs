using FolketingetVotes.Core.Enums;
using FolketingetVotes.Core.Queries;
using FolketingetVotes.Core.ReadModels;
using FolketingetVotes.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FolketingetVotes.Data.Queries;

internal sealed class CaseQueries(FolketingetDbContext db) : ICaseQueries
{
    public async Task<CaseDetail?> GetAsync(int caseId, CancellationToken cancellationToken = default)
    {
        var summary = await SummaryAsync(db, caseId, cancellationToken);
        if (summary is null)
        {
            return null;
        }

        var c = await db.Cases.AsNoTracking().SingleAsync(x => x.Id == caseId, cancellationToken);

        var actors = await (
            from ca in db.CaseActors
            join a in db.Actors on ca.ActorId equals a.Id
            join r0 in db.Lookups.Where(l => l.Kind == LookupKind.CaseActorRole) on ca.RoleId equals r0.Id into rr
            from r in rr.DefaultIfEmpty()
            where ca.CaseId == caseId
            orderby ca.RoleId, a.Name
            select new CaseActorRow(a.Id, a.Name, ca.RoleId, r != null ? r.Name : "Rolle " + ca.RoleId, a.TypeId)).ToListAsync(cancellationToken);

        var steps = await (
            from s in db.CaseSteps
            join t0 in db.Lookups.Where(l => l.Kind == LookupKind.CaseStepType) on s.TypeId equals t0.Id into tt
            from t in tt.DefaultIfEmpty()
            join st0 in db.Lookups.Where(l => l.Kind == LookupKind.CaseStepStatus) on s.StatusId equals st0.Id into sts
            from st in sts.DefaultIfEmpty()
            where s.CaseId == caseId
            orderby s.Date, s.Id
            select new CaseStepRow(s.Id, s.Title, s.Date, t != null ? t.Name : null, st != null ? st.Name : null)).ToListAsync(cancellationToken);

        var votes = (await VoteProjections.Rows(db)
            .Where(v => v.CaseId == caseId)
            .OrderBy(v => v.Date).ThenBy(v => v.VoteId)
            .ToListAsync(cancellationToken)).Select(VoteProjections.ToItem).ToList();

        var keywords = await (
            from ck in db.CaseKeywords
            join k in db.Keywords on ck.KeywordId equals k.Id
            where ck.CaseId == caseId
            orderby k.TypeId, k.Name
            select new KeywordRow(k.Id, k.Name, k.TypeId)).ToListAsync(cancellationToken);

        return new CaseDetail(summary, c.Summary, c.VotingConclusion, c.LawNumber, c.LawDate, c.RetsinformationUrl, actors, steps, votes, keywords);
    }

    public async Task<PagedResult<CaseListItem>> SearchAsync(CaseFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var query =
            from c in db.Cases
            join p in db.Periods on c.PeriodId equals p.Id
            join st0 in db.Lookups.Where(l => l.Kind == LookupKind.CaseStatus) on c.StatusId equals st0.Id into sts
            from st in sts.DefaultIfEmpty()
            select new { c, p, StatusName = st != null ? st.Name : null, VoteCount = db.Votes.Count(v => db.CaseSteps.Any(s => s.Id == v.CaseStepId && s.CaseId == c.Id)) };

        query = filter.Type is { } type
            ? query.Where(x => x.c.TypeId == type)
            : query.Where(x => x.c.TypeId == CaseType.Bill || x.c.TypeId == CaseType.Resolution);

        if (filter.PeriodId is { } periodId)
        {
            query = query.Where(x => x.c.PeriodId == periodId);
        }

        if (filter.OnlyWithVotes)
        {
            query = query.Where(x => x.VoteCount > 0);
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var pattern = VoteProjections.Pattern(filter.Query);
            query = query.Where(x => EF.Functions.ILike(x.c.Title, pattern) || EF.Functions.ILike(x.c.ShortTitle!, pattern) || EF.Functions.ILike(x.c.Number!, pattern)
                || db.CaseKeywords.Any(ck => ck.CaseId == x.c.Id && db.Keywords.Any(k => k.Id == ck.KeywordId && EF.Functions.ILike(k.Name, pattern))));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.p.StartDate).ThenBy(x => x.c.NumberPrefix).ThenByDescending(x => x.c.NumberNumeric)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new CaseListItem(x.c.Id, x.c.TypeId, x.c.Number, x.c.ShortTitle ?? x.c.Title, x.StatusName, x.p.Id, x.p.Title, x.VoteCount, null))
            .ToListAsync(cancellationToken);
        return new PagedResult<CaseListItem>(items, page, pageSize, total);
    }

    internal static Task<CaseSummary?> SummaryAsync(FolketingetDbContext db, int caseId, CancellationToken cancellationToken) =>
        (from c in db.Cases
         join p in db.Periods on c.PeriodId equals p.Id
         join s0 in db.Lookups.Where(l => l.Kind == LookupKind.CaseStatus) on c.StatusId equals s0.Id into ss
         from s in ss.DefaultIfEmpty()
         where c.Id == caseId
         select new CaseSummary(c.Id, c.TypeId, c.Title, c.ShortTitle, c.Number, c.StatusId, s != null ? s.Name : null, p.Id, p.Code, p.Title))
        .FirstOrDefaultAsync(cancellationToken);
}
