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

        return new CaseDetail(summary, c.Summary, c.VotingConclusion, c.LawNumber, c.LawDate, c.RetsinformationUrl, actors, steps, votes);
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
